using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;

namespace BilliardsManagement.Controllers
{
    public class ManagerController : Controller
    {
        private readonly BilliardsDbContext _context;

        public ManagerController(BilliardsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Check if user is manager
            var role = HttpContext.Session.GetString("Role")?.ToUpper();
            if (role != "MANAGER")
            {
                return RedirectToAction("Login", "Account");
            }

            var today = DateTime.Today;
            var model = new ManagerDashboardViewModel
            {
                Tables = await _context.Tables.ToListAsync(),
                TotalTables = await _context.Tables.CountAsync(),
                AvailableTables = await _context.Tables.CountAsync(t => t.Status == "AVAILABLE"),
                BrokenTables = await _context.Tables.CountAsync(t => t.Status == "BROKEN"),
                MaintenanceTables = await _context.Tables.CountAsync(t => t.Status == "MAINTENANCE"),
                TotalEmployees = await _context.Employees.CountAsync(),
                TotalProducts = await _context.Products.CountAsync(),
                LowStockProducts = await _context.Products.CountAsync(p => p.Quantity < 10),
                TodayRevenue = await _context.Invoices
                    .Where(i => i.PaymentTime!.Value.Date == today)
                    .SumAsync(i => i.TotalAmount ?? 0)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> TableManagement()
        {
            var tables = await _context.Tables.ToListAsync();
            return View(tables);
        }

        [HttpPost]
        public async Task<IActionResult> AddTable()
        {
            var maxTableId = await _context.Tables.MaxAsync(t => (int?)t.TableId) ?? 0;
            var nextTableNumber = maxTableId + 1;

            var newTable = new Table
            {
                TableName = $"Bàn {nextTableNumber}",
                TableType = "STANDARD",
                Status = "AVAILABLE",
                PricePerHour = 100000 // Default price 100,000 VND
            };

            _context.Tables.Add(newTable);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(TableManagement));
        }

        [HttpPost]
        public async Task<IActionResult> EditTable(int tableId, string tableType, string status, decimal pricePerHour)
        {
            var table = await _context.Tables.FindAsync(tableId);
            if (table == null)
                return NotFound();

            table.TableType = tableType;
            table.Status = status;
            // Automatically set price based on table type
            table.PricePerHour = tableType?.ToUpper() == "VIP" ? 200000 : 100000;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(TableManagement));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleTableStatus(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null)
                return NotFound();

            // Update status rotation: AVAILABLE -> MAINTENANCE -> BROKEN -> AVAILABLE
            table.Status = table.Status switch
            {
                "AVAILABLE" => "MAINTENANCE",
                "MAINTENANCE" => "BROKEN",
                _ => "AVAILABLE"
            };

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(TableManagement));
        }

        public IActionResult Revenue()
        {
            return View();
        }

        public IActionResult Inventory()
        {
            return View();
        }

        public IActionResult Staff()
        {
            return View();
        }

        public IActionResult Roles()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Booking()
        {
            try
            {
                // Cập nhật trạng thái các đơn đặt bàn đã hết hạn
                await UpdateExpiredReservations();
                
                // Get all tables
                var tables = await _context.Tables.ToListAsync();
                
                // Manually load sessions for each table to avoid serialization issues
                foreach (var table in tables)
                {
                    // Load active sessions
                    table.Sessions = await _context.Sessions
                        .Where(s => s.TableId == table.TableId && s.EndTime == null)
                        .ToListAsync();
                        
                    // Load upcoming reservations that are confirmed
                    table.Reservations = await _context.Reservations
                        .Where(r => r.TableId == table.TableId && 
                                r.EndTime > DateTime.Now && 
                                r.Status == "CONFIRMED")
                        .ToListAsync();
                }
                
                // Get all customers for dropdown
                var customers = await _context.Customers.ToListAsync();
                
                // Get upcoming reservations
                var reservations = await _context.Reservations
                    .Include(r => r.Customer)
                    .Include(r => r.Table)
                    .Where(r => r.EndTime > DateTime.Now && r.Status == "CONFIRMED")
                    .OrderBy(r => r.StartTime)
                    .ToListAsync();
                
                var viewModel = new BookingViewModel
                {
                    Tables = tables,
                    Customers = customers,
                    Reservations = reservations
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateReservation(int tableId, int customerId, string bookingDate, string startTimeStr, string endTimeStr, string notes)
        {
            // Check for null or empty values
            if (string.IsNullOrEmpty(bookingDate) || string.IsNullOrEmpty(startTimeStr) || string.IsNullOrEmpty(endTimeStr))
            {
                TempData["Error"] = "Thời gian bắt đầu và kết thúc không được để trống";
                return RedirectToAction(nameof(Booking));
            }
            
            // Verify that the customer exists
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                TempData["Error"] = "Khách hàng không tồn tại. Vui lòng chọn khách hàng khác.";
                return RedirectToAction(nameof(Booking));
            }

            // Parse the date and time components
            DateTime startDateTime, endDateTime;
            
            try
            {
                // Parse the date (yyyy-MM-dd format)
                var date = DateTime.ParseExact(bookingDate, "yyyy-MM-dd", null);
                
                // Parse the start time (HH:mm format)
                var startTimeParts = startTimeStr.Split(':');
                var startHour = int.Parse(startTimeParts[0]);
                var startMinute = int.Parse(startTimeParts[1]);
                
                // Parse the end time (HH:mm format)
                var endTimeParts = endTimeStr.Split(':');
                var endHour = int.Parse(endTimeParts[0]);
                var endMinute = int.Parse(endTimeParts[1]);
                
                // Create the full datetime objects
                startDateTime = new DateTime(date.Year, date.Month, date.Day, startHour, startMinute, 0);
                endDateTime = new DateTime(date.Year, date.Month, date.Day, endHour, endMinute, 0);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi định dạng ngày giờ: {ex.Message}";
                return RedirectToAction(nameof(Booking));
            }
            
            // Ensure dates are valid
            if (startDateTime >= endDateTime)
            {
                TempData["Error"] = "Thời gian kết thúc phải sau thời gian bắt đầu";
                return RedirectToAction(nameof(Booking));
            }

            // Make sure start time is not in the past
            if (startDateTime < DateTime.Now)
            {
                TempData["Error"] = "Không thể đặt bàn trong quá khứ";
                return RedirectToAction(nameof(Booking));
            }
            
            // Check if table is available for the requested time period
            var isTableBooked = await _context.Reservations
                .AnyAsync(r => r.TableId == tableId && 
                           r.Status == "CONFIRMED" && 
                           ((startDateTime >= r.StartTime && startDateTime < r.EndTime) ||
                            (endDateTime > r.StartTime && endDateTime <= r.EndTime) ||
                            (startDateTime <= r.StartTime && endDateTime >= r.EndTime)));
                
            if (isTableBooked)
            {
                TempData["Error"] = "Bàn đã được đặt trong khoảng thời gian này";
                return RedirectToAction(nameof(Booking));
            }

            // Check if the table is currently in use
            var isTableInUse = await _context.Tables
                .AnyAsync(t => t.TableId == tableId && t.Status == "IN_USE");

            if (isTableInUse)
            {
                TempData["Error"] = "Bàn đang được sử dụng, không thể đặt bàn";
                return RedirectToAction(nameof(Booking));
            }
            
            // Create new reservation
            var reservation = new Reservation
            {
                TableId = tableId,
                CustomerId = customerId,
                StartTime = startDateTime,
                EndTime = endDateTime,
                Status = "CONFIRMED",
                Notes = notes
            };
            
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Đặt bàn thành công";
            return RedirectToAction(nameof(Booking));
        }
        
        [HttpPost]
        public async Task<IActionResult> CancelReservation(int reservationId)
        {
            var reservation = await _context.Reservations.FindAsync(reservationId);
            if (reservation == null)
                return NotFound();
                
            reservation.Status = "CANCELLED";
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Hủy đặt bàn thành công";
            return RedirectToAction(nameof(Booking));
        }
        
        [HttpPost]
        public async Task<IActionResult> StartSession(int tableId, int? customerId = null, int? reservationId = null)
        {
            // Kiểm tra quyền - chỉ MANAGER và EMPLOYEE mới có quyền bắt đầu
            var role = HttpContext.Session.GetString("Role")?.ToUpper();
            if (role != "MANAGER" && role != "EMPLOYEE")
            {
                TempData["Error"] = "Bạn không có quyền thực hiện chức năng này";
                return RedirectToAction(nameof(Booking));
            }
            
            // Check if table is already in use
            var isTableInUse = await _context.Sessions
                .AnyAsync(s => s.TableId == tableId && s.EndTime == null);
                
            if (isTableInUse)
            {
                TempData["Error"] = "Bàn đang được sử dụng";
                return RedirectToAction(nameof(Booking));
            }
            
            int employeeId;
            
            // Get employee ID from session
            var employeeIdStr = HttpContext.Session.GetString("EmployeeId");
            if (!string.IsNullOrEmpty(employeeIdStr) && int.TryParse(employeeIdStr, out employeeId))
            {
                // Use the employee ID from session
            }
            else
            {
                // If employee ID not found in session but user is MANAGER
                if (role == "MANAGER")
                {
                    // Find the manager from the logged-in user info
                    var username = HttpContext.Session.GetString("Username");
                    if (!string.IsNullOrEmpty(username))
                    {
                        var manager = await _context.Employees
                            .FirstOrDefaultAsync(e => e.Username == username && e.Position == "MANAGER");
                        
                        if (manager != null)
                        {
                            employeeId = manager.EmployeeId;
                        }
                        else
                        {
                            // Get the first manager from database as fallback
                            var firstManager = await _context.Employees
                                .FirstOrDefaultAsync(e => e.Position == "MANAGER");
                                
                            if (firstManager != null)
                            {
                                employeeId = firstManager.EmployeeId;
                            }
                            else
                            {
                                TempData["Error"] = "Không tìm thấy người quản lý trong hệ thống";
                                return RedirectToAction(nameof(Booking));
                            }
                        }
                    }
                    else
                    {
                        // Last resort: use the first employee in database
                        var firstEmployee = await _context.Employees.FirstOrDefaultAsync();
                        
                        if (firstEmployee != null)
                        {
                            employeeId = firstEmployee.EmployeeId;
                        }
                        else
                        {
                            TempData["Error"] = "Không tìm thấy nhân viên trong hệ thống";
                            return RedirectToAction(nameof(Booking));
                        }
                    }
                }
                else
                {
                    TempData["Error"] = "Không xác định được nhân viên";
                    return RedirectToAction(nameof(Booking));
                }
            }
            
            // Create new session
            var session = new Session
            {
                TableId = tableId,
                EmployeeId = employeeId,
                StartTime = DateTime.Now,
                EndTime = null,
                TableTotal = 0,
                TotalTime = 0
            };
            
            _context.Sessions.Add(session);
            
            // If this is from a reservation, update the reservation status
            if (reservationId.HasValue)
            {
                var reservation = await _context.Reservations.FindAsync(reservationId.Value);
                if (reservation != null)
                {
                    reservation.Status = "USED";
                    // Maybe update other fields as needed
                }
            }
            
            // Update table status
            var table = await _context.Tables.FindAsync(tableId);
            if (table != null)
            {
                table.Status = "IN_USE";
            }
            
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Bắt đầu sử dụng bàn thành công";
            return RedirectToAction(nameof(Booking));
        }

        [HttpPost]
        public async Task<IActionResult> EndSession(int sessionId)
        {
            // Kiểm tra quyền - chỉ MANAGER và EMPLOYEE mới có quyền kết thúc
            var role = HttpContext.Session.GetString("Role")?.ToUpper();
            if (role != "MANAGER" && role != "EMPLOYEE")
            {
                TempData["Error"] = "Bạn không có quyền thực hiện chức năng này";
                return RedirectToAction(nameof(Booking));
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
                return RedirectToAction(nameof(Booking));
            }
            
            // Kết thúc phiên
            var now = DateTime.Now;
            session.EndTime = now;
            
            // Tính tổng thời gian sử dụng (tính chính xác bằng giờ)
            if (session.StartTime.HasValue)
            {
                var duration = now - session.StartTime.Value;
                
                // Lưu thời gian chơi chính xác (tính bằng giờ với phần thập phân)
                double totalHoursExact = duration.TotalHours;
                session.TotalTime = (int)(duration.TotalMinutes); // Vẫn lưu tổng số phút để tương thích ngược
                
                // Tính tổng tiền dựa vào loại bàn và thời gian sử dụng CHÍNH XÁC
                var pricePerHour = session.Table?.PricePerHour ?? 0;
                session.TableTotal = (decimal)(totalHoursExact * (double)pricePerHour);
            }
            else
            {
                session.TotalTime = 0;
                session.TableTotal = 0;
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
            
            // Cập nhật trạng thái bàn
            if (session.Table != null)
            {
                session.Table.Status = "AVAILABLE";
            }
            
            await _context.SaveChangesAsync();
            
            // Chuyển đến trang tạo hóa đơn
            TempData["Success"] = "Kết thúc sử dụng bàn thành công";
            return RedirectToAction(nameof(Booking));
        }

        [HttpGet]
        public async Task<IActionResult> FoodAndBeverage()
        {
            var model = new InventoryViewModel
            {
                Products = await _context.Products
                    .Where(p => p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE)
                    .OrderBy(p => p.ProductType)
                    .ToListAsync(),
                TotalValue = await _context.Products
                    .Where(p => p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE)
                    .SumAsync(p => (p.Price ?? 0) * (p.Quantity ?? 0)),
                TotalItems = await _context.Products
                    .CountAsync(p => p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE),
                LowStockItems = await _context.Products
                    .CountAsync(p => (p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE) 
                                   && p.Quantity < 10)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Equipment()
        {
            var model = new InventoryViewModel
            {
                Products = await _context.Products
                    .Where(p => p.ProductType == ProductType.EQUIPMENT || p.ProductType == ProductType.SUPPLIES)
                    .ToListAsync(),
                CurrentTab = "EQUIPMENT",
                TotalValue = await _context.Products
                    .Where(p => p.ProductType == ProductType.EQUIPMENT || p.ProductType == ProductType.SUPPLIES)
                    .SumAsync(p => (p.Price ?? 0) * (p.Quantity ?? 0)),
                TotalItems = await _context.Products
                    .CountAsync(p => p.ProductType == ProductType.EQUIPMENT || p.ProductType == ProductType.SUPPLIES),
                LowStockItems = await _context.Products
                    .CountAsync(p => (p.ProductType == ProductType.EQUIPMENT || p.ProductType == ProductType.SUPPLIES) 
                                   && p.Quantity < 10)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            return Json(new
            {
                productId = product.ProductId,
                productName = product.ProductName,
                productType = product.ProductType.ToString(),
                price = product.Price,
                quantity = product.Quantity,
                status = product.Status
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveProduct(Product product)
        {
            if (product.ProductId == 0)
            {
                _context.Products.Add(product);
            }
            else
            {
                var existingProduct = await _context.Products.FindAsync(product.ProductId);
                if (existingProduct == null)
                    return NotFound();

                existingProduct.ProductName = product.ProductName;
                existingProduct.ProductType = product.ProductType;
                existingProduct.Price = product.Price;
                existingProduct.Quantity = product.Quantity;
                existingProduct.Status = product.Status;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(product.ProductType == ProductType.FOOD || product.ProductType == ProductType.BEVERAGE 
                ? nameof(FoodAndBeverage) 
                : nameof(Equipment));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] Customer customer)
        {
            if (string.IsNullOrEmpty(customer.Phone) || string.IsNullOrEmpty(customer.FullName))
            {
                return BadRequest(new { error = "Số điện thoại và tên khách hàng là bắt buộc" });
            }

            // Check if phone number already exists
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == customer.Phone);
            if (existingCustomer != null)
            {
                return BadRequest(new { error = "Số điện thoại đã tồn tại" });
            }

            customer.LoyaltyPoints = 0; // Initialize loyalty points
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            // Log để kiểm tra ID được sinh ra
            Console.WriteLine($"Created new customer with ID: {customer.CustomerId}");

            return Ok(new { 
                customerId = customer.CustomerId,
                fullName = customer.FullName,
                phone = customer.Phone
            });
        }

        [HttpGet]
        public async Task<IActionResult> SearchCustomers(string query)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 2)
            {
                return Ok(new List<Customer>());
            }

            var customers = await _context.Customers
                .Where(c => (c.Phone != null && c.Phone.Contains(query)) || 
                           (c.FullName != null && c.FullName.Contains(query)))
                .Take(5)
                .Select(c => new { c.CustomerId, c.FullName, c.Phone })
                .ToListAsync();

            return Ok(customers);
        }

        [HttpGet]
        public async Task<IActionResult> GetUpcomingReservations()
        {
            try
            {
                // Cập nhật trạng thái các đơn đặt bàn đã hết hạn
                await UpdateExpiredReservations();
                
                var now = DateTime.Now;
                
                // Get only confirmed and upcoming reservations
                var reservations = await _context.Reservations
                    .Include(r => r.Customer)
                    .Include(r => r.Table)
                    .Where(r => r.EndTime > now && r.Status == "CONFIRMED")
                    .OrderBy(r => r.StartTime)
                    .ToListAsync();

                // Manually create a flat DTO to avoid circular references
                var result = reservations.Select(r => new
                {
                    id = r.ReservationId,
                    title = (r.Table != null && r.Customer != null) 
                        ? $"{r.Table.TableName} - {r.Customer.FullName}"
                        : "Đặt bàn",
                    start = r.StartTime,
                    end = r.EndTime,
                    resourceId = r.TableId,
                    status = r.Status,
                    tableName = r.Table != null ? r.Table.TableName : string.Empty,
                    customerName = r.Customer != null ? r.Customer.FullName : "Không xác định",
                    tableId = r.TableId,
                    customerId = r.CustomerId,
                    className = "bg-primary", // Always CONFIRMED here
                    allDay = false,
                    description = r.Notes,
                    reservationId = r.ReservationId
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
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
        public async Task<IActionResult> TableService(int tableId, int sessionId)
        {
            // Get table information
            var table = await _context.Tables.FindAsync(tableId);
            if (table == null)
                return NotFound();
                
            // Get session information
            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.EndTime == null);
            if (session == null)
                return NotFound();
                
            // Get available products
            var availableProducts = await _context.Products
                .Where(p => (p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE) 
                          && p.Status == "AVAILABLE" && p.Quantity > 0)
                .ToListAsync();
                
            // Get current order details
            var orderDetails = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                .Where(od => od.Order != null && od.Order.SessionId == sessionId && od.Order.Status == "PENDING")
                .ToListAsync();
                
            // Calculate total amount for current order
            decimal orderTotal = orderDetails.Sum(od => (od.UnitPrice ?? 0) * (od.Quantity ?? 0));
            
            // Create dictionary to track products in the order and their available quantities
            var productAvailability = new Dictionary<int, int>();
            
            // For each product in the order, calculate how many more can be added
            foreach (var product in availableProducts)
            {
                // Get the current quantity in the order, if any
                var orderDetail = orderDetails.FirstOrDefault(od => od.ProductId == product.ProductId);
                int quantityInOrder = orderDetail?.Quantity ?? 0;
                
                // Store the product ID and its available quantity (considering what's already in the order)
                productAvailability[product.ProductId] = product.Quantity ?? 0;
            }
            
            // Create view model
            var viewModel = new TableServiceViewModel
            {
                Table = table,
                Session = session,
                AvailableProducts = availableProducts,
                OrderDetails = orderDetails,
                OrderTotal = orderTotal,
                ProductAvailability = productAvailability
            };
            
            return View(viewModel);
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

            int employeeId;
            // Get employee ID from session
            var employeeIdStr = HttpContext.Session.GetString("EmployeeId");
            var role = HttpContext.Session.GetString("Role")?.ToUpper();
            
            if (!string.IsNullOrEmpty(employeeIdStr) && int.TryParse(employeeIdStr, out employeeId))
            {
                // Use the employee ID from session
            }
            else
            {
                // If employee ID not found in session but user is MANAGER
                if (role == "MANAGER")
                {
                    // Find the manager from the logged-in user info
                    var username = HttpContext.Session.GetString("Username");
                    if (!string.IsNullOrEmpty(username))
                    {
                        var manager = await _context.Employees
                            .FirstOrDefaultAsync(e => e.Username == username && e.Position == "MANAGER");
                        
                        if (manager != null)
                        {
                            employeeId = manager.EmployeeId;
                        }
                        else
                        {
                            // Get the first manager from database as fallback
                            var firstManager = await _context.Employees
                                .FirstOrDefaultAsync(e => e.Position == "MANAGER");
                                
                            if (firstManager != null)
                            {
                                employeeId = firstManager.EmployeeId;
                            }
                            else
                            {
                                return Json(new { success = false, message = "Không tìm thấy người quản lý trong hệ thống" });
                            }
                        }
                    }
                    else
                    {
                        // Last resort: use the first employee in database
                        var firstEmployee = await _context.Employees.FirstOrDefaultAsync();
                        
                        if (firstEmployee != null)
                        {
                            employeeId = firstEmployee.EmployeeId;
                        }
                        else
                        {
                            return Json(new { success = false, message = "Không tìm thấy nhân viên trong hệ thống" });
                        }
                    }
                }
                else
                {
                    return Json(new { success = false, message = "Không xác định được nhân viên" });
                }
            }
            
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

            // Check if the product already exists in the order
            var existingOrderDetail = await _context.OrderDetails
                .FirstOrDefaultAsync(od => od.OrderId == order.OrderId && od.ProductId == productId);

            if (existingOrderDetail != null)
            {
                // If the product already exists, increase its quantity
                int newQuantity = (existingOrderDetail.Quantity ?? 0) + quantity;
                
                // Check if there's enough stock for the new total quantity
                if (product.Quantity < quantity)
                {
                    return Json(new { success = false, message = "Số lượng trong kho không đủ" });
                }
                
                existingOrderDetail.Quantity = newQuantity;
            }
            else
            {
                // If the product doesn't exist in the order, create a new order detail
                var orderDetail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.Price ?? 0
                };
                
                _context.OrderDetails.Add(orderDetail);
            }

            // Update product quantity
            product.Quantity = (product.Quantity ?? 0) - quantity;
            if (product.Quantity <= 0)
                product.Status = "OUT_OF_STOCK";

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

        [HttpPost]
        public async Task<IActionResult> UpdateOrderDetailQuantity(int orderDetailId, int changeAmount)
        {
            var orderDetail = await _context.OrderDetails
                .Include(od => od.Product)
                .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

            if (orderDetail == null)
                return Json(new { success = false, message = "Không tìm thấy chi tiết đơn hàng" });

            int newQuantity = (orderDetail.Quantity ?? 0) + changeAmount;

            // Xử lý trường hợp giảm số lượng
            if (changeAmount < 0)
            {
                // Cập nhật lại số lượng trong kho (trả lại số lượng sản phẩm)
                if (orderDetail.Product != null)
                {
                    orderDetail.Product.Quantity = (orderDetail.Product.Quantity ?? 0) - changeAmount; // Trừ với số âm = cộng
                    if (orderDetail.Product.Quantity > 0)
                        orderDetail.Product.Status = "AVAILABLE";
                }
                
                // Nếu số lượng mới là 0 hoặc âm, xóa mục này khỏi đơn hàng
                if (newQuantity <= 0)
                {
                    _context.OrderDetails.Remove(orderDetail);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, removed = true });
                }
            }
            else if (changeAmount > 0) // Xử lý trường hợp tăng số lượng
            {
                // Kiểm tra xem còn đủ hàng trong kho không
                if (orderDetail.Product == null || orderDetail.Product.Quantity == null)
                {
                    return Json(new { success = false, message = "Không thể xác định số lượng tồn kho" });
                }
                
                if (orderDetail.Product.Quantity < changeAmount)
                {
                    return Json(new { 
                        success = false, 
                        message = $"Số lượng trong kho không đủ. Chỉ còn {orderDetail.Product.Quantity} {orderDetail.Product.ProductName}" 
                    });
                }
                
                // Cập nhật lại số lượng trong kho (giảm số lượng)
                orderDetail.Product.Quantity = orderDetail.Product.Quantity - changeAmount;
                if (orderDetail.Product.Quantity <= 0)
                    orderDetail.Product.Status = "OUT_OF_STOCK";
            }
            
            // Cập nhật số lượng mới cho chi tiết đơn hàng
            orderDetail.Quantity = newQuantity;
            await _context.SaveChangesAsync();
            
            return Json(new { 
                success = true, 
                newQuantity = newQuantity,
                subtotal = newQuantity * (orderDetail.UnitPrice ?? 0)
            });
        }
        
        [HttpGet]
        public async Task<IActionResult> GetOrderDetailInfo()
        {
            // Get all order details with their product information
            var orderDetails = await _context.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.Order.Status == "PENDING")
                .Select(od => new
                {
                    orderDetailId = od.OrderDetailId,
                    productId = od.ProductId,
                    productName = od.Product.ProductName,
                    currentQuantity = od.Quantity,
                    availableQuantity = od.Product.Quantity,
                    unitPrice = od.UnitPrice
                })
                .ToListAsync();

            return Json(orderDetails);
        }
        
        // Cập nhật trạng thái các đơn đặt bàn đã hết hạn
        private async Task UpdateExpiredReservations()
        {
            try
            {
                var now = DateTime.Now;
                
                // Lấy tất cả các đơn đặt bàn đã hết hạn nhưng vẫn ở trạng thái CONFIRMED
                var expiredReservations = await _context.Reservations
                    .Where(r => r.EndTime <= now && r.Status == "CONFIRMED")
                    .ToListAsync();
                
                if (expiredReservations.Any())
                {
                    foreach (var reservation in expiredReservations)
                    {
                        // Cập nhật trạng thái thành EXPIRED
                        reservation.Status = "EXPIRED";
                    }
                    
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Đã cập nhật {expiredReservations.Count} đơn đặt bàn hết hạn");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi cập nhật đơn đặt bàn hết hạn: {ex.Message}");
            }
        }
    }
}