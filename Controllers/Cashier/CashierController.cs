using Microsoft.AspNetCore.Mvc;
using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;
using BilliardsManagement.Services;
using BilliardsManagement.Attributes;
using Microsoft.EntityFrameworkCore;

namespace BilliardsManagement.Controllers.Cashier
{
    public class CashierController : Controller
    {
        private readonly BilliardsDbContext _context;
        private readonly IPermissionService _permissionService;
        private readonly IInvoiceService _invoiceService;
        private readonly ITransactionService _transactionService;
        private readonly ITableService _tableService;

        public CashierController(BilliardsDbContext context, IPermissionService permissionService, IInvoiceService invoiceService, ITransactionService transactionService, ITableService tableService)
        {
            _context = context;
            _permissionService = permissionService;
            _invoiceService = invoiceService;
            _transactionService = transactionService;
            _tableService = tableService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userRole = HttpContext.Session.GetString("Role");
            var employeeId = HttpContext.Session.GetInt32("EmployeeId") ?? 0;

            if (userRole != "CASHIER")
            {
                return RedirectToAction("Index", "Home");
            }
            
            // Check for error message from RequirePermissionAttribute
            var tempError = HttpContext.Session.GetString("TempError");
            if (!string.IsNullOrEmpty(tempError))
            {
                TempData["Error"] = tempError;
                HttpContext.Session.Remove("TempError");
            }

            var viewModel = new CashierDashboardViewModel();

            // Get user permissions
            if (employeeId > 0)
            {
                viewModel.UserPermissions = await _permissionService.GetEmployeePermissionsAsync(employeeId);
            }

            // Get dashboard statistics
            var today = DateTime.Today;
            
            // Table statistics
            var totalTables = await _context.Tables.CountAsync();
            var activeTables = await _context.Tables.CountAsync(t => t.Status == "OCCUPIED");
            viewModel.TotalActiveTables = activeTables;
            viewModel.TotalAvailableTables = totalTables - activeTables;

            // Today's revenue and transactions
            var todayInvoices = await _context.Invoices
                .Where(i => i.PaymentTime.HasValue && i.PaymentTime.Value.Date == today && i.Status == "COMPLETED")
                .ToListAsync();

            viewModel.TodayRevenue = todayInvoices.Sum(i => i.TotalAmount ?? 0);
            viewModel.TodayTransactions = todayInvoices.Count;

            // Pending orders count (if has permission)
            if (viewModel.CanManageOrders)
            {
                viewModel.PendingOrders = await _context.Orders
                    .CountAsync(o => o.Status == "PENDING");
            }

            // Get table status for quick view
            viewModel.TableStatus = await _context.Tables
                .Select(t => new TableQuickInfo
                {
                    TableId = t.TableId,
                    TableNumber = t.TableName ?? "",
                    Status = t.Status ?? "",
                    PlayerName = "", // We'll need to adjust this based on your table structure
                    StartTime = null, // We'll need to adjust this based on your table structure
                    CurrentBill = _context.Invoices
                        .Where(i => i.Session != null && i.Session.TableId == t.TableId && i.Status == "PENDING")
                        .Sum(i => i.TotalAmount ?? 0)
                })
                .Take(8)
                .ToListAsync();

            // Get recent transactions
            viewModel.RecentTransactions = await _context.Invoices
                .Include(i => i.Session)
                .ThenInclude(s => s.Table)
                .Where(i => i.PaymentTime.HasValue && i.PaymentTime.Value.Date == today)
                .OrderByDescending(i => i.PaymentTime)
                .Take(5)
                .Select(i => new RecentTransaction
                {
                    InvoiceId = i.InvoiceId,
                    TableNumber = i.Session != null && i.Session.Table != null ? i.Session.Table.TableName ?? "" : "",
                    Amount = i.TotalAmount ?? 0,
                    CreatedAt = i.PaymentTime ?? DateTime.MinValue,
                    Status = i.Status ?? ""
                })
                .ToListAsync();

            // Get pending orders list (if has permission)
            if (viewModel.CanManageOrders)
            {
                viewModel.PendingOrdersList = await _context.Orders
                    .Include(o => o.Session)
                    .ThenInclude(s => s.Table)
                    .Where(o => o.Status == "PENDING")
                    .OrderBy(o => o.CreatedAt)
                    .Take(10)
                    .Select(o => new PendingOrderInfo
                    {
                        OrderId = o.OrderId,
                        TableNumber = o.Session != null && o.Session.Table != null ? o.Session.Table.TableName ?? "" : "",
                        ProductName = "Đơn hàng", // We'll need to adjust this based on your order structure
                        Quantity = 1, // We'll need to adjust this based on your order structure
                        OrderTime = o.CreatedAt ?? DateTime.MinValue,
                        Status = o.Status ?? ""
                    })
                    .ToListAsync();
            }

            return View(viewModel);
        }

        [HttpPost]
        [RequirePermission("PAYMENT_PROCESS")]
        public async Task<IActionResult> ProcessPayment(int invoiceId)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Session)
                    .ThenInclude(s => s.Table)
                    .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

                if (invoice == null)
                {
                    TempData["Error"] = "Không tìm thấy hóa đơn!";
                    return RedirectToAction("Index");
                }

                if (invoice.Status == "COMPLETED")
                {
                    TempData["Error"] = "Hóa đơn này đã được thanh toán!";
                    return RedirectToAction("Index");
                }

                // Update invoice status
                invoice.Status = "COMPLETED";
                invoice.PaymentTime = DateTime.Now;

                // Update table status if session and table exist
                if (invoice.Session?.Table != null)
                {
                    invoice.Session.Table.Status = "AVAILABLE";
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Thanh toán thành công hóa đơn {invoiceId}!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi xử lý thanh toán!";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CompleteOrder(int orderId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                order.Status = "COMPLETED";
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã hoàn thành đơn hàng!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra!" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTableInfo(int tableId)
        {
            try
            {
                var table = await _context.Tables
                    .Include(t => t.Sessions.Where(s => s.EndTime == null))
                    .ThenInclude(s => s.Invoices.Where(i => i.Status == "PENDING"))
                    .FirstOrDefaultAsync(t => t.TableId == tableId);

                if (table == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bàn!" });
                }

                var currentSession = table.Sessions.FirstOrDefault(s => s.EndTime == null);
                var pendingInvoice = currentSession?.Invoices.FirstOrDefault(i => i.Status == "PENDING");

                var tableInfo = new
                {
                    tableId = table.TableId,
                    tableNumber = table.TableName ?? "",
                    status = table.Status ?? "",
                    playerName = "", // Adjust based on your data structure
                    startTime = currentSession?.StartTime?.ToString("HH:mm dd/MM/yyyy"),
                    invoice = pendingInvoice != null ? new
                    {
                        invoiceId = pendingInvoice.InvoiceId,
                        totalAmount = pendingInvoice.TotalAmount ?? 0,
                        orders = new object[] { } // We'll return empty array for now since OrderDetails relationship is complex
                    } : null
                };

                return Json(new { success = true, data = tableInfo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra!" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingInvoices()
        {
            try
            {
                // First, let's check how many active sessions we have
                var activeSessions = await _context.Sessions
                    .Include(s => s.Table)
                    .Where(s => s.EndTime == null)
                    .ToListAsync();

                Console.WriteLine($"DEBUG: Found {activeSessions.Count} active sessions");
                foreach (var session in activeSessions)
                {
                    Console.WriteLine($"  Session {session.SessionId}: Table {session.Table?.TableName}, Start: {session.StartTime}");
                }

                var pendingInvoices = await _context.Invoices
                    .Include(i => i.Session)
                    .ThenInclude(s => s.Table)
                    .Include(i => i.Session)
                    .ThenInclude(s => s.Orders)
                    .ThenInclude(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .Where(i => i.Status == "PENDING" && 
                               i.Session != null && 
                               i.Session.EndTime == null) // IMPORTANT: Only active sessions
                    .OrderBy(i => i.Session.StartTime)
                    .ToListAsync();

                Console.WriteLine($"DEBUG: Found {pendingInvoices.Count} pending invoices for active sessions");
                foreach (var invoice in pendingInvoices)
                {
                    Console.WriteLine($"  Invoice {invoice.InvoiceId}: Session {invoice.SessionId}, Status: {invoice.Status}");
                }

                var invoiceDataTasks = pendingInvoices.Select(async invoice => {
                    var currentDuration = invoice.Session?.StartTime != null 
                        ? DateTime.Now - invoice.Session.StartTime.Value
                        : TimeSpan.Zero;
                    
                    // Use InvoiceService for accurate calculations
                    var (tableTotal, serviceTotal, grandTotal) = await _invoiceService.CalculateInvoiceTotalsByInvoiceIdAsync(invoice.InvoiceId);
                    
                    return new
                    {
                        invoiceId = invoice.InvoiceId,
                        tableId = invoice.Session?.TableId ?? 0,
                        tableName = invoice.Session?.Table?.TableName?.Replace("Bàn ", "") ?? "N/A",
                        sessionId = invoice.Session?.SessionId ?? 0,
                        startTime = invoice.Session?.StartTime?.ToString("HH:mm dd/MM") ?? "",
                        currentDuration = FormatDuration(currentDuration),
                        pricePerHour = invoice.Session?.Table?.PricePerHour ?? 0,
                        currentTableTotal = tableTotal,
                        serviceTotal = serviceTotal,
                        grandTotal = grandTotal
                    };
                });

                var invoiceData = await Task.WhenAll(invoiceDataTasks);

                Console.WriteLine($"DEBUG: Returning {invoiceData.Length} invoice data items");

                return Json(new { success = true, data = invoiceData });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG ERROR in GetPendingInvoices: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Helper method to format duration
        private string FormatDuration(TimeSpan duration)
        {
            int hours = (int)duration.TotalHours;
            int minutes = duration.Minutes;
            return $"{hours}h {minutes}m";
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoiceDetails(int invoiceId)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Session)
                    .ThenInclude(s => s.Table)
                    .Include(i => i.Session)
                    .ThenInclude(s => s.Orders)
                    .ThenInclude(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

                if (invoice == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy hóa đơn!" });
                }

                var duration = invoice.Session?.StartTime != null 
                    ? DateTime.Now - invoice.Session.StartTime.Value
                    : TimeSpan.Zero;

                // Use InvoiceService for accurate calculations
                var (tableTotal, serviceTotal, grandTotal) = await _invoiceService.CalculateInvoiceTotalsByInvoiceIdAsync(invoiceId);
                var orderDetails = await _invoiceService.GetInvoiceOrderDetailsByInvoiceIdAsync(invoiceId);

                var result = new
                {
                    invoiceId = invoice.InvoiceId,
                    tableId = invoice.Session?.TableId ?? 0,
                    tableName = invoice.Session?.Table?.TableName?.Replace("Bàn ", "") ?? "N/A", // Remove "Bàn " prefix
                    tableType = invoice.Session?.Table?.TableType ?? "STANDARD",
                    startTime = invoice.Session?.StartTime?.ToString("HH:mm dd/MM/yyyy") ?? "",
                    currentDuration = FormatDuration(duration),
                    pricePerHour = invoice.Session?.Table?.PricePerHour ?? 0,
                    tableTotal = tableTotal,
                    orderDetails = orderDetails,
                    serviceTotal = serviceTotal,
                    grandTotal = grandTotal
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("PAYMENT_PROCESS")]
        public async Task<IActionResult> ProcessPaymentWithCalculation(int invoiceId, string paymentMethod = "CASH")
        {
            try
            {
                var cashierId = HttpContext.Session.GetInt32("EmployeeId");
                if (!cashierId.HasValue)
                {
                    return Json(new { success = false, message = "Không xác định được nhân viên!" });
                }

                // Validate payment method
                var validPaymentMethods = new[] { "CASH", "CARD", "QR_PAY" };
                if (!validPaymentMethods.Contains(paymentMethod))
                {
                    return Json(new { success = false, message = "Phương thức thanh toán không hợp lệ!" });
                }

                var invoice = await _context.Invoices
                    .Include(i => i.Session)
                    .ThenInclude(s => s.Table)
                    .Include(i => i.Session)
                    .ThenInclude(s => s.Orders)
                    .ThenInclude(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.Status == "PENDING");

                if (invoice == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy hóa đơn chưa thanh toán!" });
                }

                if (invoice.Session?.EndTime != null)
                {
                    return Json(new { success = false, message = "Phiên chơi đã kết thúc!" });
                }

                // Calculate current totals
                var now = DateTime.Now;
                var duration = invoice.Session?.StartTime != null 
                    ? now - invoice.Session.StartTime.Value 
                    : TimeSpan.Zero;

                // Use InvoiceService for accurate calculations
                var (tableTotal, orderTotal, grandTotal) = await _invoiceService.CalculateInvoiceTotalsByInvoiceIdAsync(invoiceId);

                // End the session
                if (invoice.Session != null)
                {
                    invoice.Session.EndTime = now;
                    invoice.Session.TotalTime = (int)duration.TotalMinutes;
                    invoice.Session.TableTotal = tableTotal;

                    // Mark all orders as completed
                    foreach (var order in invoice.Session.Orders ?? new List<BilliardsManagement.Models.Entities.Order>())
                    {
                        order.Status = "COMPLETED";
                    }

                    // Update table status to available
                    if (invoice.Session.Table != null)
                    {
                        invoice.Session.Table.Status = "AVAILABLE";
                    }
                }

                // Update invoice
                invoice.CashierId = cashierId.Value;
                invoice.TotalAmount = grandTotal;
                invoice.PaymentTime = now;
                invoice.PaymentMethod = paymentMethod;
                invoice.Status = "COMPLETED";

                await _context.SaveChangesAsync();

                return Json(new 
                { 
                    success = true, 
                    message = "Thanh toán thành công!",
                    data = new 
                    {
                        invoiceId = invoice.InvoiceId,
                        tableName = invoice.Session?.Table?.TableName?.Replace("Bàn ", "") ?? "N/A",
                        totalAmount = grandTotal,
                        paymentMethod = paymentMethod,
                        paymentTime = now.ToString("HH:mm dd/MM/yyyy")
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DebugActiveSessions()
        {
            try
            {
                var data = await _invoiceService.GetActiveSessionsDebugInfoAsync();
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateMissingInvoices()
        {
            var (success, message, createdCount) = await _invoiceService.CreateMissingInvoicesAsync();
            
            return Json(new { 
                success = success, 
                message = message,
                createdCount = createdCount
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentTransactions(int days = 7, int limit = 20)
        {
            try
            {
                var data = await _transactionService.GetRecentTransactionsAsync(days, limit);
                
                return Json(new
                {
                    success = true,
                    data = data
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Table Transfer functionality for Cashier
        [HttpGet]
        [RequirePermission("TABLE_TRANSFER")]
        public async Task<IActionResult> TableTransfer()
        {
            var occupiedTables = await _tableService.GetOccupiedTablesWithSessionsAsync();
            var availableTables = await _tableService.GetAvailableTablesAsync();

            var viewModel = new TableTransferViewModel
            {
                OccupiedTables = occupiedTables,
                AvailableTables = availableTables
            };

            return View(viewModel);
        }

        [HttpPost]
        [RequirePermission("TABLE_TRANSFER")]
        public async Task<IActionResult> TransferTable(int fromTableId, int toTableId)
        {
            // Get employee ID for audit trail
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");
            if (!employeeId.HasValue)
            {
                return Json(new { success = false, message = "Không xác định được nhân viên thực hiện" });
            }

            var success = await _tableService.TransferTableAsync(fromTableId, toTableId, employeeId.Value);
            
            if (success)
            {
                // Get table names for response
                var fromTable = await _tableService.GetTableByIdAsync(fromTableId);
                var toTable = await _tableService.GetTableByIdAsync(toTableId);
                
                return Json(new 
                { 
                    success = true, 
                    message = $"Đã chuyển thành công từ {fromTable?.TableName} sang {toTable?.TableName}",
                    data = new 
                    {
                        fromTable = fromTable?.TableName,
                        toTable = toTable?.TableName,
                        transferTime = DateTime.Now
                    }
                });
            }
            
            return Json(new { success = false, message = "Không thể chuyển bàn. Vui lòng kiểm tra lại." });
        }

        [HttpGet]
        public async Task<IActionResult> GetTableTransferInfo(int tableId)
        {
            var result = await _tableService.GetTableTransferInfoAsync(tableId);
            
            if (result == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin bàn hoặc bàn không có phiên hoạt động" });
            }
            
            return Json(new { success = true, data = result });
        }
    }
}