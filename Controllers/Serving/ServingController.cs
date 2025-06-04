using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;
using BilliardsManagement.Attributes;
using BilliardsManagement.Services;

namespace BilliardsManagement.Controllers;

public class ServingController : Controller
{
    private readonly BilliardsDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly ISessionService _sessionService;
    private readonly IOrderManagementService _orderManagementService;
    private readonly IInvoiceService _invoiceService;
    private readonly ITableService _tableService;
    private readonly IBookingService _bookingService;
    private readonly ICustomerService _customerService;
    private readonly IProductService _productService;

    public ServingController(
        BilliardsDbContext context, 
        IPermissionService permissionService,
        ISessionService sessionService,
        IOrderManagementService orderManagementService,
        IInvoiceService invoiceService,
        ITableService tableService,
        IBookingService bookingService,
        ICustomerService customerService,
        IProductService productService)
    {
        _context = context;
        _permissionService = permissionService;
        _sessionService = sessionService;
        _orderManagementService = orderManagementService;
        _invoiceService = invoiceService;
        _tableService = tableService;
        _bookingService = bookingService;
        _customerService = customerService;
        _productService = productService;
    }

    public async Task<IActionResult> Index()
    {
        var role = HttpContext.Session.GetString("Role")?.ToUpper();
        if (role != "SERVING")
        {
            return RedirectToAction("Login", "Account");
        }
        
        // Check for error message from RequirePermissionAttribute
        var tempError = HttpContext.Session.GetString("TempError");
        if (!string.IsNullOrEmpty(tempError))
        {
            TempData["Error"] = tempError;
            HttpContext.Session.Remove("TempError");
        }

        var tables = await _context.Tables
            .Include(t => t.Sessions.Where(s => s.EndTime == null))
            .OrderBy(t => t.TableId)
            .ToListAsync();

        // Check if user has TABLE_TRANSFER permission
        var employeeId = HttpContext.Session.GetInt32("EmployeeId");
        bool hasTableTransferPermission = false;
        
        if (employeeId.HasValue)
        {
            hasTableTransferPermission = await _permissionService.HasPermissionAsync(employeeId.Value, "TABLE_TRANSFER");
        }
        
        ViewBag.HasTableTransferPermission = hasTableTransferPermission;

        return View(tables);
    }

    public async Task<IActionResult> Table(int id)
    {
        var model = new ServingOrderViewModel
        {
            TableId = id,
            TableName = await _context.Tables
                .Where(t => t.TableId == id)
                .Select(t => t.TableName)
                .FirstOrDefaultAsync() ?? string.Empty,
            AvailableProducts = await _context.Products
                .Where(p => (p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE) 
                           && p.Status == "AVAILABLE")
                .ToListAsync(),
            SessionId = await _context.Sessions
                .Where(s => s.TableId == id && s.EndTime == null)
                .Select(s => s.SessionId)
                .FirstOrDefaultAsync()
        };

        if (model.SessionId.HasValue)
        {
            var currentOrders = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                .Where(od => od.Order != null && od.Order.SessionId == model.SessionId)
                .ToListAsync();

            model.CurrentOrders = currentOrders;
            model.TotalAmount = currentOrders.Sum(od => od.UnitPrice * od.Quantity);
        }

        return View("Table_Information", model);
    }

    [HttpPost]
    public async Task<IActionResult> AddOrder(int tableId, int productId, int quantity)
    {
        var employeeId = HttpContext.Session.GetInt32("EmployeeId");
        if (!employeeId.HasValue)
            return Json(new { success = false, message = "Không xác định được nhân viên" });

        var result = await _orderManagementService.AddOrderAsync(tableId, productId, quantity, employeeId.Value);
        
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveOrderDetail(int orderDetailId)
    {
        var result = await _orderManagementService.RemoveOrderDetailAsync(orderDetailId);
        
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpGet]
    public async Task<IActionResult> StartTable(int id)
    {
        var employeeId = HttpContext.Session.GetInt32("EmployeeId");
        if (!employeeId.HasValue)
        {
            TempData["Error"] = "Không xác định được nhân viên";
            return RedirectToAction(nameof(Index));
        }

        var result = await _sessionService.StartSessionAsync(id, employeeId.Value);
        
        if (result.Success)
        {
            TempData["Success"] = "Bắt đầu sử dụng bàn thành công";
            return RedirectToAction("Table", new { id });
        }
        else
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableProducts()
    {
        var products = await _productService.GetAvailableProductsAsync();
        return Json(products);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetCurrentOrder(int sessionId)
    {
        var orderDetails = await _orderManagementService.GetOrderDetailsBySessionAsync(sessionId);
        var result = orderDetails.Select(od => new {
            orderDetailId = od.OrderDetailId,
            productName = od.Product?.ProductName,
            quantity = od.Quantity,
            unitPrice = od.UnitPrice
        }).ToList();
        
        return Json(result);
    }
    
    [HttpGet]
    public async Task<IActionResult> EndSession(int sessionId)
    {
        // Kiểm tra quyền
        var role = HttpContext.Session.GetString("Role")?.ToUpper();
        if (role != "SERVING" && role != "MANAGER" && role != "EMPLOYEE")
        {
            TempData["Error"] = "Bạn không có quyền thực hiện chức năng này";
            return RedirectToAction(nameof(Index));
        }

        var employeeId = HttpContext.Session.GetInt32("EmployeeId");
        if (!employeeId.HasValue)
        {
            TempData["Error"] = "Không xác định được nhân viên";
            return RedirectToAction(nameof(Index));
        }

        var result = await _sessionService.EndSessionAsync(sessionId);
        
        if (result.Success)
        {
            TempData["Success"] = "Kết thúc phiên sử dụng bàn thành công";
        }
        else
        {
            TempData["Error"] = result.Message;
        }
        
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [RequirePermission("PAYMENT_PROCESS")]
    public async Task<IActionResult> GetPendingInvoices()
    {
        try
        {
            var pendingInvoices = await _context.Invoices
                .Include(i => i.Session)
                .ThenInclude(s => s.Table)
                .Where(i => i.Status == "PENDING" && 
                           i.Session != null && 
                           i.Session.EndTime == null)
                .OrderBy(i => i.Session.StartTime)
                .ToListAsync();

            var invoiceData = new List<object>();
            
            foreach (var invoice in pendingInvoices)
            {
                var currentDuration = invoice.Session?.StartTime != null 
                    ? DateTime.Now - invoice.Session.StartTime.Value
                    : TimeSpan.Zero;

                // Use InvoiceService for accurate calculations
                var (tableTotal, serviceTotal, grandTotal) = await _invoiceService.CalculateInvoiceTotalsAsync(invoice.Session!.SessionId);
                
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
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [RequirePermission("PAYMENT_PROCESS")]
    public async Task<IActionResult> ProcessPayment(int invoiceId, string paymentMethod = "CASH")
    {
        try
        {
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");
            if (!employeeId.HasValue)
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
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.Status == "PENDING");

            if (invoice == null)
            {
                return Json(new { success = false, message = "Không tìm thấy hóa đơn chưa thanh toán!" });
            }

            if (invoice.Session?.EndTime != null)
            {
                return Json(new { success = false, message = "Phiên chơi đã kết thúc!" });
            }

            // Use InvoiceService for accurate calculations
            var (tableTotal, serviceTotal, grandTotal) = await _invoiceService.CalculateInvoiceTotalsByInvoiceIdAsync(invoiceId);

            // End the session using SessionService
            var endResult = await _sessionService.EndSessionAsync(invoice.Session!.SessionId);
            if (!endResult.Success)
            {
                return Json(new { success = false, message = endResult.Message });
            }

            // Update invoice
            invoice.CashierId = employeeId.Value;
            invoice.TotalAmount = grandTotal;
            invoice.PaymentTime = DateTime.Now;
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
                    paymentTime = DateTime.Now.ToString("HH:mm dd/MM/yyyy")
                }
            });
        }
        catch (Exception ex)
        {
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

    // Booking functionality
    [HttpGet]
    public async Task<IActionResult> Booking()
    {
        var viewModel = await _bookingService.GetBookingDataAsync();
        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> CreateReservation(int tableId, int customerId, DateTime bookingDate, string startTimeStr, string endTimeStr, string notes = "")
    {
        var result = await _bookingService.CreateReservationAsync(tableId, customerId, bookingDate.ToString("yyyy-MM-dd"), startTimeStr, endTimeStr, notes);
        
        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }
        
        return RedirectToAction("Booking");
    }

    [HttpPost]
    public async Task<IActionResult> StartSession(int tableId, int? reservationId = null, int? customerId = null)
    {
        var employeeId = HttpContext.Session.GetInt32("EmployeeId");
        if (!employeeId.HasValue)
        {
            TempData["Error"] = "Không xác định được nhân viên";
            return RedirectToAction("Booking");
        }

        var result = await _bookingService.StartSessionAsync(tableId, employeeId.Value, customerId, reservationId);
        
        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }
        
        return RedirectToAction("Booking");
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] Customer customer)
    {
        var result = await _customerService.CreateCustomerAsync(customer);
        
        if (!result.Success)
        {
            return BadRequest(new { error = result.Message });
        }

        return Ok(new { 
            customerId = result.Customer!.CustomerId,
            fullName = result.Customer!.FullName,
            phone = result.Customer!.Phone
        });
    }

    // Table Transfer functionality (same as Manager)
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
        else
        {
            return Json(new { success = false, message = "Không thể chuyển bàn. Vui lòng kiểm tra lại." });
        }
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