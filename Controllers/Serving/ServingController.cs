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
}