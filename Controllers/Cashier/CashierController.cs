using Microsoft.AspNetCore.Mvc;
using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;
using BilliardsManagement.Services;
using BilliardsManagement.Attributes;
using BilliardsManagement.Repositories;
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
        
        // Add Repository dependencies
        private readonly ITableRepository _tableRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IOrderRepository _orderRepository;

        public CashierController(
            BilliardsDbContext context, 
            IPermissionService permissionService, 
            IInvoiceService invoiceService, 
            ITransactionService transactionService, 
            ITableService tableService,
            ITableRepository tableRepository,
            IInvoiceRepository invoiceRepository,
            IOrderRepository orderRepository)
        {
            _context = context;
            _permissionService = permissionService;
            _invoiceService = invoiceService;
            _transactionService = transactionService;
            _tableService = tableService;
            _tableRepository = tableRepository;
            _invoiceRepository = invoiceRepository;
            _orderRepository = orderRepository;
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
            var totalTables = await _tableRepository.GetCountAsync();
            var activeTables = await _tableRepository.GetCountByStatusAsync("OCCUPIED");
            viewModel.TotalActiveTables = activeTables;
            viewModel.TotalAvailableTables = totalTables - activeTables;

            // Today's revenue and transactions
            var todayInvoices = await _invoiceRepository.GetTodayInvoicesAsync();

            viewModel.TodayRevenue = todayInvoices.Sum(i => i.TotalAmount ?? 0);
            viewModel.TodayTransactions = todayInvoices.Count;

            // Pending orders count (if has permission)
            if (viewModel.CanManageOrders)
            {
                viewModel.PendingOrders = await _orderRepository.GetPendingCountAsync();
            }

            // Get table status for quick view
            var tables = await _tableRepository.GetAllAsync();
            viewModel.TableStatus = tables
                .Take(8)
                .Select(t => new TableQuickInfo
                {
                    TableId = t.TableId,
                    TableNumber = t.TableName ?? "",
                    Status = t.Status ?? "",
                    PlayerName = "", // We'll need to adjust this based on your table structure
                    StartTime = null, // We'll need to adjust this based on your table structure
                    CurrentBill = 0 // We'll set this to 0 for now to avoid complex queries
                })
                .ToList();

            // Get recent transactions
            var recentInvoices = await _invoiceRepository.GetByDateRangeAsync(today, today);
            viewModel.RecentTransactions = recentInvoices
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
                .ToList();

            // Get pending orders list (if has permission)
            if (viewModel.CanManageOrders)
            {
                var pendingOrders = await _orderRepository.GetPendingOrdersAsync();
                viewModel.PendingOrdersList = pendingOrders
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
                    .ToList();
            }

            return View(viewModel);
        }

        [HttpPost]
        [RequirePermission("PAYMENT_PROCESS")]
        public async Task<IActionResult> ProcessPayment(int invoiceId)
        {
            try
            {
                var invoice = await _invoiceRepository.GetByIdWithDetailsAsync(invoiceId);

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

                await _invoiceRepository.UpdateAsync(invoice);

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
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                order.Status = "COMPLETED";
                await _orderRepository.UpdateAsync(order);

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
                var table = await _tableRepository.GetByIdAsync(tableId);

                if (table == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bàn!" });
                }

                // Get pending invoice for this table if exists
                var pendingInvoice = await _context.Invoices
                    .Include(i => i.Session)
                    .Where(i => i.Session != null && 
                               i.Session.TableId == tableId && 
                               i.Session.EndTime == null && 
                               i.Status == "PENDING")
                    .FirstOrDefaultAsync();

                var currentSession = pendingInvoice?.Session;

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
                var pendingInvoices = await _invoiceRepository.GetPendingWithActiveSessionsAsync();

                var invoiceData = new List<object>();
                
                // Process invoices sequentially to avoid DbContext threading issues
                foreach (var invoice in pendingInvoices)
                {
                    var currentDuration = invoice.Session?.StartTime != null 
                        ? DateTime.Now - invoice.Session.StartTime.Value
                        : TimeSpan.Zero;
                    
                    // Use InvoiceService for accurate calculations
                    var (tableTotal, serviceTotal, grandTotal) = await _invoiceService.CalculateInvoiceTotalsByInvoiceIdAsync(invoice.InvoiceId);
                    
                    invoiceData.Add(new
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
                    });
                }

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
                var invoice = await _invoiceRepository.GetByIdWithDetailsAsync(invoiceId);

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
                Console.WriteLine($"DEBUG: ProcessPaymentWithCalculation called - InvoiceId: {invoiceId}, PaymentMethod: {paymentMethod}");
                
                var cashierId = HttpContext.Session.GetInt32("EmployeeId");
                if (!cashierId.HasValue)
                {
                    Console.WriteLine("DEBUG: No cashier ID found in session");
                    return Json(new { success = false, message = "Không xác định được nhân viên!" });
                }

                Console.WriteLine($"DEBUG: Cashier ID: {cashierId.Value}");

                // Validate payment method
                var validPaymentMethods = new[] { "CASH", "CARD", "QR_PAY" };
                if (!validPaymentMethods.Contains(paymentMethod))
                {
                    Console.WriteLine($"DEBUG: Invalid payment method: {paymentMethod}");
                    return Json(new { success = false, message = "Phương thức thanh toán không hợp lệ!" });
                }

                Console.WriteLine("DEBUG: Getting invoice from repository");
                var invoice = await _invoiceRepository.GetByIdWithDetailsAsync(invoiceId);

                if (invoice == null || invoice.Status != "PENDING")
                {
                    Console.WriteLine($"DEBUG: Invoice not found or not pending. Invoice: {invoice?.InvoiceId}, Status: {invoice?.Status}");
                    return Json(new { success = false, message = "Không tìm thấy hóa đơn chưa thanh toán!" });
                }

                if (invoice.Session?.EndTime != null)
                {
                    Console.WriteLine("DEBUG: Session already ended");
                    return Json(new { success = false, message = "Phiên chơi đã kết thúc!" });
                }

                Console.WriteLine($"DEBUG: Invoice found - ID: {invoice.InvoiceId}, SessionId: {invoice.SessionId}");

                // Calculate current totals
                var now = DateTime.Now;
                var duration = invoice.Session?.StartTime != null 
                    ? now - invoice.Session.StartTime.Value 
                    : TimeSpan.Zero;

                Console.WriteLine($"DEBUG: Session duration: {duration.TotalMinutes} minutes");

                // Use InvoiceService for accurate calculations
                Console.WriteLine("DEBUG: Calculating invoice totals");
                var (tableTotal, orderTotal, grandTotal) = await _invoiceService.CalculateInvoiceTotalsByInvoiceIdAsync(invoiceId);

                Console.WriteLine($"DEBUG: Calculated totals - Table: {tableTotal}, Order: {orderTotal}, Grand: {grandTotal}");

                // End the session
                if (invoice.Session != null)
                {
                    Console.WriteLine("DEBUG: Ending session");
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
                        Console.WriteLine($"DEBUG: Updating table {invoice.Session.Table.TableId} status to AVAILABLE");
                        invoice.Session.Table.Status = "AVAILABLE";
                    }
                }

                // Update invoice
                Console.WriteLine("DEBUG: Updating invoice");
                invoice.CashierId = cashierId.Value;
                invoice.TotalAmount = grandTotal;
                invoice.PaymentTime = now;
                invoice.PaymentMethod = paymentMethod;
                invoice.Status = "COMPLETED";

                Console.WriteLine("DEBUG: Saving invoice changes");
                await _invoiceRepository.UpdateAsync(invoice);

                Console.WriteLine("DEBUG: Payment completed successfully");
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
                Console.WriteLine($"DEBUG ERROR in ProcessPaymentWithCalculation: {ex.Message}");
                Console.WriteLine($"DEBUG ERROR Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Lỗi xử lý thanh toán: {ex.Message}" });
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

        [HttpGet]
        public async Task<IActionResult> DebugInvoicesAndSessions()
        {
            try
            {
                var allInvoices = await _context.Invoices
                    .Include(i => i.Session)
                    .ThenInclude(s => s.Table)
                    .ToListAsync();

                var allSessions = await _context.Sessions
                    .Include(s => s.Table)
                    .ToListAsync();

                var debugData = new
                {
                    totalInvoices = allInvoices.Count,
                    pendingInvoices = allInvoices.Count(i => i.Status == "PENDING"),
                    completedInvoices = allInvoices.Count(i => i.Status == "COMPLETED"),
                    totalSessions = allSessions.Count,
                    activeSessions = allSessions.Count(s => s.EndTime == null),
                    completedSessions = allSessions.Count(s => s.EndTime != null),
                    invoices = allInvoices.Select(i => new
                    {
                        invoiceId = i.InvoiceId,
                        sessionId = i.SessionId,
                        status = i.Status,
                        totalAmount = i.TotalAmount,
                        paymentTime = i.PaymentTime,
                        tableName = i.Session?.Table?.TableName ?? "N/A",
                        sessionActive = i.Session?.EndTime == null
                    }),
                    sessions = allSessions.Select(s => new
                    {
                        sessionId = s.SessionId,
                        tableId = s.TableId,
                        tableName = s.Table?.TableName ?? "N/A",
                        startTime = s.StartTime,
                        endTime = s.EndTime,
                        isActive = s.EndTime == null,
                        hasInvoice = allInvoices.Any(inv => inv.SessionId == s.SessionId)
                    })
                };

                return Json(new { success = true, data = debugData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}