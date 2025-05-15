using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;

namespace BilliardsManagement.Controllers;

public class ServingController : Controller
{
    private readonly BilliardsDbContext _context;

    public ServingController(BilliardsDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var role = HttpContext.Session.GetString("Role")?.ToUpper();
        if (role != "SERVING")
        {
            return RedirectToAction("Login", "Account");
        }

        var tables = await _context.Tables
            .Include(t => t.Sessions.Where(s => s.EndTime == null))
            .ToListAsync();

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
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.TableId == tableId && s.EndTime == null);

        if (session == null)
            return Json(new { success = false, message = "Không tìm thấy phiên hoạt động" });

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return Json(new { success = false, message = "Không tìm thấy sản phẩm" });

        if (product.Quantity < quantity)
            return Json(new { success = false, message = "Số lượng trong kho không đủ" });

        var employeeIdStr = HttpContext.Session.GetString("EmployeeId");
        if (string.IsNullOrEmpty(employeeIdStr) || !int.TryParse(employeeIdStr, out var employeeId))
            return Json(new { success = false, message = "Không xác định được nhân viên" });
        
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.SessionId == session.SessionId && o.Status == "PENDING");

        if (order == null)
        {
            order = new Order
            {
                SessionId = session.SessionId,
                EmployeeId = employeeId,
                Status = "PENDING"
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
        }

        var orderDetail = new OrderDetail
        {
            OrderId = order.OrderId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = product.Price ?? 0
        };

        product.Quantity = (product.Quantity ?? 0) - quantity;
        if (product.Quantity <= 0)
            product.Status = "OUT_OF_STOCK";

        _context.OrderDetails.Add(orderDetail);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveOrderDetail(int orderDetailId)
    {
        var orderDetail = await _context.OrderDetails
            .Include(od => od.Product)
            .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

        if (orderDetail == null)
            return Json(new { success = false, message = "Không tìm thấy chi tiết đơn hàng" });

        if (orderDetail.Product != null)
        {
            orderDetail.Product.Quantity = (orderDetail.Product.Quantity ?? 0) + orderDetail.Quantity;
            if (orderDetail.Product.Quantity > 0)
                orderDetail.Product.Status = "AVAILABLE";
        }

        _context.OrderDetails.Remove(orderDetail);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> StartTable(int id)
    {
        // Check if table is already in use
        var isTableInUse = await _context.Sessions
            .AnyAsync(s => s.TableId == id && s.EndTime == null);
            
        if (isTableInUse)
        {
            TempData["Error"] = "Bàn đang được sử dụng";
            return RedirectToAction(nameof(Index));
        }
        
        var employeeIdStr = HttpContext.Session.GetString("EmployeeId");
        if (string.IsNullOrEmpty(employeeIdStr) || !int.TryParse(employeeIdStr, out var employeeId))
        {
            TempData["Error"] = "Không xác định được nhân viên";
            return RedirectToAction(nameof(Index));
        }
        
        // Create new session
        var session = new Session
        {
            TableId = id,
            EmployeeId = employeeId,
            StartTime = DateTime.Now,
            EndTime = null,
            TableTotal = 0,
            TotalTime = 0
        };
        
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();
        
        // Create an initial pending order for this session
        var order = new Order
        {
            SessionId = session.SessionId,
            EmployeeId = employeeId,
            CreatedAt = DateTime.Now,
            Status = "PENDING"
        };
        
        _context.Orders.Add(order);
        
        // Update table status
        var table = await _context.Tables.FindAsync(id);
        if (table != null)
        {
            table.Status = "IN_USE";
        }
        
        await _context.SaveChangesAsync();
        
        TempData["Success"] = "Bắt đầu sử dụng bàn thành công";
        return RedirectToAction("Table", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableProducts()
    {
        var products = await _context.Products
            .Where(p => (p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE) 
                      && p.Status == "AVAILABLE" && p.Quantity > 0)
            .Select(p => new {
                productId = p.ProductId,
                productName = p.ProductName,
                productType = p.ProductType.ToString(),
                price = p.Price,
                quantity = p.Quantity
            })
            .ToListAsync();
            
        return Json(products);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetCurrentOrder(int sessionId)
    {
        var orderDetails = await _context.OrderDetails
            .Include(od => od.Order)
            .Include(od => od.Product)
            .Where(od => od.Order != null && od.Order.SessionId == sessionId && od.Order.Status == "PENDING")
            .Select(od => new {
                orderDetailId = od.OrderDetailId,
                productName = od.Product.ProductName,
                quantity = od.Quantity,
                unitPrice = od.UnitPrice
            })
            .ToListAsync();
            
        return Json(orderDetails);
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
        
        // Tìm phiên hiện tại
        var session = await _context.Sessions
            .Include(s => s.Table)
            .Include(s => s.Orders)
                .ThenInclude(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.EndTime == null);
                
        if (session == null)
        {
            TempData["Error"] = "Không tìm thấy phiên sử dụng hoặc phiên đã kết thúc";
            return RedirectToAction(nameof(Index));
        }
        
        // Kết thúc phiên
        var now = DateTime.Now;
        session.EndTime = now;
        
        // Tính tổng thời gian sử dụng (tính chính xác bằng giờ)
        if (session.StartTime.HasValue)
        {
            var timeUsed = now - session.StartTime.Value;
            session.TotalTime = (decimal)timeUsed.TotalHours;
            
            // Tính tiền bàn
            if (session.Table?.PricePerHour.HasValue == true)
            {
                session.TableTotal = session.Table.PricePerHour.Value * (decimal)timeUsed.TotalHours;
            }
        }
        
        // Tính tổng tiền dịch vụ
        decimal orderTotal = 0;
        foreach (var order in session.Orders)
        {
            foreach (var detail in order.OrderDetails)
            {
                orderTotal += (detail.UnitPrice ?? 0) * (detail.Quantity ?? 0);
            }
            
            // Cập nhật trạng thái đơn hàng
            order.Status = "COMPLETED";
        }
        
        // Tạo hóa đơn
        var employeeIdStr = HttpContext.Session.GetString("EmployeeId");
        if (!string.IsNullOrEmpty(employeeIdStr) && int.TryParse(employeeIdStr, out var employeeId))
        {
            var invoice = new Invoice
            {
                SessionId = sessionId,
                CashierId = employeeId,
                TotalAmount = (session.TableTotal ?? 0) + orderTotal,
                PaymentTime = now,
                PaymentMethod = "CASH", // Mặc định là tiền mặt
                Status = "PAID",
                Discount = 0
            };
            
            _context.Invoices.Add(invoice);
        }
        
        // Cập nhật trạng thái bàn
        if (session.Table != null)
        {
            session.Table.Status = "AVAILABLE";
        }
        
        await _context.SaveChangesAsync();
        
        TempData["Success"] = "Kết thúc phiên sử dụng bàn thành công";
        return RedirectToAction(nameof(Index));
    }
}