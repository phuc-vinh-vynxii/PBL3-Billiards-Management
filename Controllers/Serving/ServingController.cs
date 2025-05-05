using Microsoft.AspNetCore.Mvc;
using BilliardsManagement.Models.ViewModels;
using BilliardsManagement.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BilliardsManagement.Controllers
{
    public class ServingController : Controller
    {
        private readonly BilliardsDbContext _context;

        public ServingController(BilliardsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var now = DateTime.Now;
            
            // Get active sessions first - including those that haven't ended yet
            var activeSessions = await _context.Sessions
                .Include(s => s.Table)
                .Where(s => s.EndTime == null || s.EndTime > now)
                .ToListAsync();

            // Get all tables and update their status based on active sessions
            var tables = await _context.Tables.ToListAsync();
            foreach (var table in tables)
            {
                if (activeSessions.Any(s => s.TableId == table.TableId))
                {
                    // If table has an active session, ensure it's marked as in use
                    table.Status = "IN_USE";
                }
            }

            var model = new WaiterDashboardViewModel
            {
                Tables = tables,
                ActiveSessions = activeSessions,
                RecentOrders = await _context.Orders
                    .Include(o => o.Session!).ThenInclude(s => s!.Table)
                    .Where(o => o.CreatedAt != null && o.CreatedAt.Value.Date == today)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .ToListAsync() ?? new List<Order>()
            };

            // Calculate table statistics
            model.AvailableTableCount = model.Tables.Count(t => t.Status == "AVAILABLE");
            model.OccupiedTableCount = model.Tables.Count(t => t.Status == "IN_USE");
            model.BookedTableCount = model.Tables.Count(t => t.Status == "BOOKED");

            // Calculate today's revenue
            var todayInvoices = await _context.Invoices
                .Where(o => o.PaymentTime != null && o.PaymentTime.Value.Date == today)
                .ToListAsync();
            model.TodayRevenue = todayInvoices.Sum(i => i.TotalAmount ?? 0);

            await _context.SaveChangesAsync();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Table_Information(int id)
        {
            var table = await _context.Tables
                .FirstOrDefaultAsync(t => t.TableId == id);

            if (table == null)
                return NotFound();

            var activeSession = await _context.Sessions
                .Where(s => s.TableId == id && (s.EndTime == null || s.EndTime > DateTime.Now))
                .Include(s => s.Orders)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync();

            var availableProducts = await _context.Products
                .Where(p => p.Status == "AVAILABLE" && p.Quantity > 0)
                .ToListAsync();

            var model = new TableInfoViewModel
            {
                TableId = table.TableId,
                TableName = table.TableName,
                TableType = table.TableType,
                Status = table.Status,
                PricePerHour = table.PricePerHour,
                AvailableProducts = availableProducts
            };

            if (activeSession != null)
            {
                var now = DateTime.Now;
                var duration = now - activeSession.StartTime;
                decimal hours = (decimal)(duration?.TotalHours ?? 0);
                
                model.SessionId = activeSession.SessionId;
                model.StartTime = activeSession.StartTime;
                model.TotalTime = hours;
                model.TableTotal = hours * (table.PricePerHour ?? 0);
                model.Orders = activeSession.Orders.ToList();
                model.EmployeeId = activeSession.EmployeeId;

                // Calculate total amount (table + products)
                decimal productsTotal = activeSession.Orders
                    .SelectMany(o => o.OrderDetails)
                    .Sum(od => (od.UnitPrice ?? 0) * (od.Quantity ?? 0));
                model.TotalAmount = model.TableTotal + productsTotal;
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> StartSession(int tableId)
        {
            var table = await _context.Tables.FindAsync(tableId);
            if (table == null)
                return NotFound();

            if (table.Status != "AVAILABLE")
                return BadRequest("Bàn không khả dụng");

            var employeeId = HttpContext.Session.GetInt32("EmployeeId");
            if (employeeId == null)
                return BadRequest("Không tìm thấy thông tin nhân viên");

            // Create new session
            var session = new Session
            {
                TableId = tableId,
                EmployeeId = employeeId,
                StartTime = DateTime.Now,
            };

            // Update table status
            table.Status = "IN_USE";

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrder(int sessionId, int productId, int quantity)
        {
            try
            {
                var session = await _context.Sessions
                    .Include(s => s.Orders)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                    return Json(new { success = false, message = "Không tìm thấy phiên" });

                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });

                if (product.Quantity < quantity)
                    return Json(new { success = false, message = "Số lượng sản phẩm không đủ" });

                var employeeId = HttpContext.Session.GetInt32("EmployeeId");
                if (employeeId == null)
                    return Json(new { success = false, message = "Không tìm thấy thông tin nhân viên" });

                // Create new order if needed or use existing one
                var order = session.Orders.FirstOrDefault(o => o.Status == "PENDING") ?? new Order
                {
                    SessionId = sessionId,
                    EmployeeId = employeeId,
                    CreatedAt = DateTime.Now,
                    Status = "PENDING"
                };

                if (order.OrderId == 0)
                {
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync(); // Save to get OrderId
                }

                // Add order detail
                var orderDetail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.Price
                };

                // Update product quantity
                product.Quantity -= quantity;

                _context.OrderDetails.Add(orderDetail);
                await _context.SaveChangesAsync();

                Response.ContentType = "application/json";
                return new JsonResult(new { 
                    success = true,
                    productName = product.ProductName,
                    price = product.Price,
                    quantity = quantity,
                    total = product.Price * quantity
                });
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error adding order: {ex.Message}");
                Response.ContentType = "application/json";
                return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi thêm sản phẩm. Vui lòng thử lại." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateInvoice(int sessionId)
        {
            var session = await _context.Sessions
                .Include(s => s.Table)
                .Include(s => s.Orders)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return NotFound("Không tìm thấy phiên");

            var employeeId = HttpContext.Session.GetInt32("EmployeeId");
            if (employeeId == null)
                return BadRequest("Không tìm thấy thông tin nhân viên");

            // Calculate total amount
            var now = DateTime.Now;
            var duration = now - session.StartTime;
            decimal hours = (decimal)(duration?.TotalHours ?? 0);
            decimal tableTotal = hours * (session.Table?.PricePerHour ?? 0);

            decimal productsTotal = session.Orders
                .SelectMany(o => o.OrderDetails)
                .Sum(od => (od.UnitPrice ?? 0) * (od.Quantity ?? 0));

            // Create invoice
            var invoice = new Invoice
            {
                SessionId = sessionId,
                CashierId = employeeId.Value,
                TotalAmount = tableTotal + productsTotal,
                PaymentTime = now,
                Status = "COMPLETED"
            };

            // End session
            session.EndTime = now;
            session.TableTotal = tableTotal;
            session.TotalTime = hours;

            // Update table status
            if (session.Table != null)
                session.Table.Status = "AVAILABLE";

            // Update order status from PENDING to SERVED
            foreach (var order in session.Orders)
                order.Status = "SERVED";

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}