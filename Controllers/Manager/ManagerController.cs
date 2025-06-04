using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;
using BilliardsManagement.Services;
using BilliardsManagement.Attributes;
using System.Text;

namespace BilliardsManagement.Controllers
{
    public class ManagerController : Controller
    {
        private readonly BilliardsDbContext _context;
        private readonly IRevenueService _revenueService;
        private readonly IPermissionService _permissionService;
        private readonly ITableService _tableService;
        private readonly IStaffService _staffService;
        private readonly IBookingService _bookingService;
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly ISessionService _sessionService;
        private readonly IOrderManagementService _orderManagementService;
        private readonly IInvoiceService _invoiceService;

        public ManagerController(
            BilliardsDbContext context, 
            IRevenueService revenueService, 
            IPermissionService permissionService,
            ITableService tableService,
            IStaffService staffService,
            IBookingService bookingService,
            IProductService productService,
            IOrderService orderService,
            ICustomerService customerService,
            ISessionService sessionService,
            IOrderManagementService orderManagementService,
            IInvoiceService invoiceService)
        {
            _context = context;
            _revenueService = revenueService;
            _permissionService = permissionService;
            _tableService = tableService;
            _staffService = staffService;
            _bookingService = bookingService;
            _productService = productService;
            _orderService = orderService;
            _customerService = customerService;
            _sessionService = sessionService;
            _orderManagementService = orderManagementService;
            _invoiceService = invoiceService;
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
                    .Where(i => i.PaymentTime!.Value.Date == today && i.Status == "COMPLETED")
                    .SumAsync(i => i.TotalAmount ?? 0)
            };

            return View(model);
        }

        [HttpGet]
        [RequirePermission("TABLE_VIEW", "TABLE_MANAGE")]
        public async Task<IActionResult> TableManagement()
        {
            var tables = await _tableService.GetAllTablesAsync();
            return View(tables);
        }

        [HttpPost]
        [RequirePermission("TABLE_MANAGE")]
        public async Task<IActionResult> AddTable()
        {
            await _tableService.AddTableAsync();
            return RedirectToAction(nameof(TableManagement));
        }

        [HttpPost]
        [RequirePermission("TABLE_MANAGE")]
        public async Task<IActionResult> EditTable(int tableId, string tableType, string status, decimal pricePerHour)
        {
            var success = await _tableService.UpdateTableAsync(tableId, tableType, status, pricePerHour);
            if (!success)
                return NotFound();

            return RedirectToAction(nameof(TableManagement));
        }

        [HttpPost]
        [RequirePermission("TABLE_MANAGE")]
        public async Task<IActionResult> ToggleTableStatus(int id)
        {
            var success = await _tableService.ToggleTableStatusAsync(id);
            if (!success)
                return NotFound();

            return RedirectToAction(nameof(TableManagement));
        }

        [HttpGet]
        [RequirePermission("REVENUE_VIEW")]
        public async Task<IActionResult> Revenue(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var viewModel = await _revenueService.GetRevenueDataAsync(fromDate, toDate);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }

        [HttpGet]
        [RequirePermission("REVENUE_VIEW")]
        public async Task<IActionResult> GetRevenueData(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var data = await _revenueService.GetRevenueDataAsync(fromDate, toDate);
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        totalRevenue = data.TotalRevenue,
                        tableRevenue = data.TableRevenue,
                        productRevenue = data.ProductRevenue,
                        todayRevenue = data.TodayRevenue,
                        weekRevenue = data.WeekRevenue,
                        monthRevenue = data.MonthRevenue,
                        yearRevenue = data.YearRevenue,
                        totalInvoices = data.TotalInvoices,
                        todayInvoices = data.TodayInvoices,
                        averageInvoiceValue = data.AverageInvoiceValue,
                        dailyRevenueData = data.DailyRevenueData,
                        monthlyRevenueData = data.MonthlyRevenueData,
                        topProductsRevenue = data.TopProductsRevenue,
                        tableRevenueData = data.TableRevenueData,
                        recentInvoices = data.RecentInvoices
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [RequirePermission("REVENUE_EXPORT")]
        public async Task<IActionResult> ExportRevenueReport(DateTime? fromDate = null, DateTime? toDate = null, string format = "json")
        {
            try
            {
                var data = await _revenueService.GetRevenueDataAsync(fromDate, toDate);
                
                if (format.ToLower() == "csv")
                {
                    var csv = GenerateCSVReport(data);
                    // Thêm BOM (Byte Order Mark) cho UTF-8 để Excel hiển thị tiếng Việt đúng
                    var preamble = Encoding.UTF8.GetPreamble();
                    var csvBytes = Encoding.UTF8.GetBytes(csv);
                    var csvWithBOM = new byte[preamble.Length + csvBytes.Length];
                    Array.Copy(preamble, 0, csvWithBOM, 0, preamble.Length);
                    Array.Copy(csvBytes, 0, csvWithBOM, preamble.Length, csvBytes.Length);
                    
                    return File(csvWithBOM, "text/csv; charset=utf-8", $"bao_cao_doanh_thu_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                }
                
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string GenerateCSVReport(RevenueViewModel data)
        {
            var csv = new System.Text.StringBuilder();
            
            // Header với thông tin báo cáo
            csv.AppendLine("BÁOOO CÁO DOANH THU");
            csv.AppendLine($"Từ ngày:,{data.FromDate:dd/MM/yyyy}");
            csv.AppendLine($"Đến ngày:,{data.ToDate:dd/MM/yyyy}");
            csv.AppendLine($"Thời gian xuất:,{DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            csv.AppendLine("");
            
            // Thống kê tổng quan
            csv.AppendLine("TỔNG QUAN DOANH THU");
            csv.AppendLine("Chỉ tiêu,Giá trị");
            csv.AppendLine($"Tổng doanh thu,\"{data.TotalRevenue:N0} VNĐ\"");
            csv.AppendLine($"Doanh thu từ bàn,\"{data.TableRevenue:N0} VNĐ\"");
            csv.AppendLine($"Doanh thu từ sản phẩm,\"{data.ProductRevenue:N0} VNĐ\"");
            csv.AppendLine($"Tổng số hóa đơn,{data.TotalInvoices}");
            csv.AppendLine($"Giá trị trung bình mỗi hóa đơn,\"{data.AverageInvoiceValue:N0} VNĐ\"");
            csv.AppendLine("");
            
            // Top sản phẩm bán chạy
            csv.AppendLine("TOP SẢN PHẨM BÁN CHẠY");
            csv.AppendLine("Tên sản phẩm,Loại sản phẩm,Số lượng bán,Doanh thu,Tỷ lệ %");
            foreach (var product in data.TopProductsRevenue)
            {
                var productTypeName = product.ProductType switch
                {
                    "FOOD" => "Thức ăn",
                    "BEVERAGE" => "Thức uống", 
                    "EQUIPMENT" => "Thiết bị",
                    _ => product.ProductType
                };
                csv.AppendLine($"\"{product.ProductName}\",\"{productTypeName}\",{product.QuantitySold},\"{product.Revenue:N0} VNĐ\",{product.Percentage:F1}%");
            }
            csv.AppendLine("");
            
            // Doanh thu theo bàn
            csv.AppendLine("DOANH THU THEO BÀN");
            csv.AppendLine("Tên bàn,Loại bàn,Số phiên chơi,Tổng thời gian (giờ),Doanh thu,Tỷ lệ %");
            foreach (var table in data.TableRevenueData)
            {
                var tableTypeName = table.TableType switch
                {
                    "STANDARD" => "Bàn thường",
                    "VIP" => "Bàn VIP",
                    "PREMIUM" => "Bàn cao cấp",
                    _ => table.TableType
                };
                csv.AppendLine($"\"{table.TableName}\",\"{tableTypeName}\",{table.TotalSessions},{table.TotalHours:F1},\"{table.Revenue:N0} VNĐ\",{table.Percentage:F1}%");
            }
            csv.AppendLine("");
            
            // Doanh thu theo ngày (nếu có dữ liệu chi tiết)
            if (data.DailyRevenueData != null && data.DailyRevenueData.Any())
            {
                csv.AppendLine("DOANH THU THEO NGÀY");
                csv.AppendLine("Ngày,Doanh thu bàn,Doanh thu sản phẩm,Tổng doanh thu,Số hóa đơn");
                foreach (var daily in data.DailyRevenueData.OrderBy(d => d.Date))
                {
                    csv.AppendLine($"{daily.Date:dd/MM/yyyy},\"{daily.TableRevenue:N0} VNĐ\",\"{daily.ProductRevenue:N0} VNĐ\",\"{daily.TotalRevenue:N0} VNĐ\",{daily.InvoiceCount}");
                }
                csv.AppendLine("");
            }
            
            // Footer
            csv.AppendLine("---");
            csv.AppendLine($"Báo cáo được tạo bởi: Hệ thống quản lý Billiards");
            csv.AppendLine($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            
            return csv.ToString();
        }

        // Action để tạo dữ liệu mẫu cho testing (chỉ dùng trong development)
        [HttpPost]
        public async Task<IActionResult> SeedRevenueData()
        {
            try
            {
                // Tạo một vài session hoàn thành với thời gian ngẫu nhiên trong tháng qua
                var random = new Random();
                var tables = await _context.Tables.ToListAsync();
                var employees = await _context.Employees.ToListAsync();
                var products = await _context.Products
                    .Where(p => p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE)
                    .ToListAsync();
                
                if (tables.Any() && employees.Any())
                {
                    // Tạo 20 session hoàn thành trong tháng qua
                    for (int i = 0; i < 20; i++)
                    {
                        var startTime = DateTime.Now.AddDays(-random.Next(1, 30)).AddHours(-random.Next(1, 10));
                        var endTime = startTime.AddHours(random.Next(1, 4)).AddMinutes(random.Next(0, 60));
                        var table = tables[random.Next(tables.Count)];
                        var employee = employees[random.Next(employees.Count)];
                        
                        // Tính thời gian chơi
                        var duration = endTime - startTime;
                        var hours = duration.TotalHours;
                        var tableTotal = (decimal)(hours * (double)(table.PricePerHour ?? 100000));
                        
                        var session = new Session
                        {
                            TableId = table.TableId,
                            EmployeeId = employee.EmployeeId,
                            StartTime = startTime,
                            EndTime = endTime,
                            TotalTime = (int)duration.TotalMinutes,
                            TableTotal = tableTotal
                        };
                        
                        _context.Sessions.Add(session);
                        await _context.SaveChangesAsync();
                        
                        // Tạo order và order details ngẫu nhiên cho session
                        decimal productTotal = 0;
                        if (products.Any() && random.Next(3) > 0) // 2/3 khả năng có order sản phẩm
                        {
                            var order = new Order
                            {
                                SessionId = session.SessionId,
                                EmployeeId = employee.EmployeeId,
                                Status = "COMPLETED",
                                CreatedAt = startTime
                            };
                            
                            _context.Orders.Add(order);
                            await _context.SaveChangesAsync();
                            
                            // Thêm 1-3 sản phẩm ngẫu nhiên
                            var productCount = random.Next(1, 4);
                            for (int j = 0; j < productCount; j++)
                            {
                                var product = products[random.Next(products.Count)];
                                var quantity = random.Next(1, 5);
                                var unitPrice = product.Price ?? 0;
                                
                                var orderDetail = new OrderDetail
                                {
                                    OrderId = order.OrderId,
                                    ProductId = product.ProductId,
                                    Quantity = quantity,
                                    UnitPrice = unitPrice
                                };
                                
                                _context.OrderDetails.Add(orderDetail);
                                productTotal += unitPrice * quantity;
                            }
                            
                            await _context.SaveChangesAsync();
                        }
                        
                        // Tạo hóa đơn cho session này (QUAN TRỌNG - để tính doanh thu)
                        var invoice = new Invoice
                        {
                            SessionId = session.SessionId,
                            CashierId = employee.EmployeeId,
                            TotalAmount = tableTotal + productTotal,
                            PaymentTime = endTime.AddMinutes(random.Next(5, 30)), // Thanh toán sau khi kết thúc 5-30 phút
                            PaymentMethod = random.Next(2) == 0 ? "CASH" : "CARD",
                            Status = "COMPLETED", // QUAN TRỌNG: Đảm bảo status là COMPLETED
                            Discount = 0
                        };
                        
                        _context.Invoices.Add(invoice);
                    }
                    
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Đã tạo dữ liệu mẫu thành công!";
                }
                else
                {
                    TempData["Error"] = "Cần có bàn và nhân viên trong hệ thống trước khi tạo dữ liệu mẫu";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi tạo dữ liệu mẫu: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Revenue));
        }

        public IActionResult Inventory()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Staff()
        {
            // Check if user is manager
            var role = HttpContext.Session.GetString("Role")?.ToUpper();
            if (role != "MANAGER")
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = await _staffService.GetStaffManagementDataAsync();
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployee(int id)
        {
            var employee = await _staffService.GetEmployeeByIdAsync(id);
            if (employee == null)
                return NotFound();

            return Json(new
            {
                employeeId = employee.EmployeeId,
                fullName = employee.FullName,
                position = employee.Position,
                phone = employee.Phone,
                email = employee.Email,
                username = employee.Username,
                createdAt = employee.CreatedAt?.ToString("yyyy-MM-dd")
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateEmployee(CreateEmployeeViewModel model)
        {
            var result = await _staffService.CreateEmployeeAsync(model);
            
            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Staff));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEmployee(EditEmployeeViewModel model)
        {
            var result = await _staffService.UpdateEmployeeAsync(model);
            
            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Staff));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return Json(new { success = false, message = "Không tìm thấy nhân viên" });
            }

            // Check if trying to delete a MANAGER - not allowed
            if (employee.Position == "MANAGER")
            {
                return Json(new { success = false, message = "Không thể xóa người quản lý" });
            }

            try
            {
                // Handle related records before deleting employee
                
                // 1. Update Invoices - set CashierId to null (keep invoice for record but remove employee reference)
                var relatedInvoices = await _context.Invoices.Where(i => i.CashierId == id).ToListAsync();
                foreach (var invoice in relatedInvoices)
                {
                    invoice.CashierId = null; // Keep invoice for audit trail but remove employee reference
                }

                // 2. Update Sessions - set EmployeeId to null (keep session for record but remove employee reference)
                var relatedSessions = await _context.Sessions.Where(s => s.EmployeeId == id).ToListAsync();
                foreach (var session in relatedSessions)
                {
                    session.EmployeeId = null; // Keep session for audit trail but remove employee reference
                }

                // 3. Update Orders - set EmployeeId to null (keep order for record but remove employee reference)
                var relatedOrders = await _context.Orders.Where(o => o.EmployeeId == id).ToListAsync();
                foreach (var order in relatedOrders)
                {
                    order.EmployeeId = null; // Keep order for audit trail but remove employee reference
                }

                // 4. Remove employee permissions
                var employeePermissions = await _context.EmployeePermissions.Where(ep => ep.EmployeeId == id).ToListAsync();
                _context.EmployeePermissions.RemoveRange(employeePermissions);

                // Save changes to update related records
                await _context.SaveChangesAsync();

                // 5. Finally delete the employee
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Đã xóa nhân viên {employee.FullName} thành công. Các dữ liệu liên quan đã được cập nhật để giữ lại lịch sử giao dịch." 
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = $"Có lỗi xảy ra khi xóa nhân viên: {ex.Message}" 
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự" });
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return Json(new { success = false, message = "Không tìm thấy nhân viên" });
            }

            // Check if trying to reset password for a MANAGER - not allowed
            if (employee.Position == "MANAGER")
            {
                return Json(new { success = false, message = "Không thể đặt lại mật khẩu cho người quản lý" });
            }

            employee.Password = BilliardsManagement.Helpers.PasswordHasher.ComputeSha256Hash(newPassword);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đặt lại mật khẩu thành công" });
        }

        [HttpGet]
        [RequirePermission("BOOKING_VIEW")]
        public async Task<IActionResult> Booking()
        {
            try
            {
                var viewModel = await _bookingService.GetBookingDataAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }

        [HttpPost]
        [RequirePermission("BOOKING_CREATE")]
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

            // Check if the table exists
            var table = await _context.Tables.FindAsync(tableId);
            if (table == null)
            {
                TempData["Error"] = "Bàn không tồn tại";
                return RedirectToAction(nameof(Booking));
            }
            
            // If table is in use, check if reservation is for the future (not conflicting with current session)
            if (table.Status == "IN_USE" && startDateTime <= DateTime.Now)
            {
                TempData["Error"] = "Bàn đang được sử dụng, chỉ có thể đặt bàn cho thời gian trong tương lai";
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
        [RequirePermission("BOOKING_CANCEL")]
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
        [RequirePermission("SESSION_START")]
        public async Task<IActionResult> StartSession(int tableId, int? customerId = null, int? reservationId = null)
        {
            // Get employee ID from session
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");
            var role = HttpContext.Session.GetString("Role")?.ToUpper();
            var username = HttpContext.Session.GetString("Username");
            
            // Resolve employee ID using the service
            var resolvedEmployeeId = await _sessionService.ResolveEmployeeIdAsync(employeeId, username, role);
            
            if (!resolvedEmployeeId.HasValue)
            {
                TempData["Error"] = "Không xác định được nhân viên";
                return RedirectToAction(nameof(Booking));
            }
            
            var result = await _sessionService.StartSessionAsync(tableId, resolvedEmployeeId.Value, customerId, reservationId);
            
            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }
            
            return RedirectToAction(nameof(Booking));
        }

        [HttpPost]
        [RequirePermission("SESSION_END")]
        public async Task<IActionResult> EndSession(int sessionId)
        {
            var result = await _sessionService.EndSessionAsync(sessionId);
            
            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }
            
            return RedirectToAction(nameof(Booking));
        }

        [HttpGet]
        [RequirePermission("PRODUCT_VIEW")]
        public async Task<IActionResult> FoodAndBeverage()
        {
            var model = await _productService.GetFoodAndBeverageInventoryAsync();
            return View(model);
        }

        [HttpGet]
        [RequirePermission("PRODUCT_VIEW")]
        public async Task<IActionResult> Equipment()
        {
            var model = await _productService.GetEquipmentInventoryAsync();
            return View(model);
        }

        [HttpGet]
        [RequirePermission("PRODUCT_VIEW")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
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
        [RequirePermission("PRODUCT_MANAGE")]
        public async Task<IActionResult> SaveProduct(Product product)
        {
            var success = await _productService.SaveProductAsync(product);
            if (!success)
                return NotFound();

            return RedirectToAction(product.ProductType == ProductType.FOOD || product.ProductType == ProductType.BEVERAGE 
                ? nameof(FoodAndBeverage) 
                : nameof(Equipment));
        }

        [HttpPost]
        [RequirePermission("PRODUCT_MANAGE")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var success = await _productService.DeleteProductAsync(id);
            if (!success)
                return NotFound();

            return Ok();
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

        [HttpGet]
        public async Task<IActionResult> SearchCustomers(string query)
        {
            var customers = await _customerService.SearchCustomersAsync(query);
            return Ok(customers);
        }

        [HttpGet]
        public async Task<IActionResult> GetUpcomingReservations()
        {
            try
            {
                var reservations = await _bookingService.GetUpcomingReservationsAsync();
                return Ok(reservations);
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
            // Get employee ID from session
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");
            var role = HttpContext.Session.GetString("Role")?.ToUpper();
            var username = HttpContext.Session.GetString("Username");
            
            // Resolve employee ID using the service
            var resolvedEmployeeId = await _sessionService.ResolveEmployeeIdAsync(employeeId, username, role);
            
            if (!resolvedEmployeeId.HasValue)
            {
                return Json(new { success = false, message = "Không xác định được nhân viên" });
            }
            
            var result = await _orderManagementService.AddOrderAsync(tableId, productId, quantity, resolvedEmployeeId.Value);
            
            return Json(new { 
                success = result.Success, 
                message = result.Message 
            });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveOrderDetail(int orderDetailId)
        {
            var result = await _orderManagementService.RemoveOrderDetailAsync(orderDetailId);
            
            return Json(new { 
                success = result.Success, 
                message = result.Message 
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderDetailQuantity(int orderDetailId, int changeAmount)
        {
            var result = await _orderManagementService.UpdateOrderDetailQuantityAsync(orderDetailId, changeAmount);
            
            return Json(new { 
                success = result.Success, 
                message = result.Message 
            });
        }
        
        [HttpGet]
        public async Task<IActionResult> GetOrderDetailInfo()
        {
            var orderDetails = await _orderService.GetOrderDetailInfoAsync();
            return Json(orderDetails);
        }

        [HttpGet]
        public async Task<IActionResult> DebugInvoiceData()
        {
            try
            {
                // Kiểm tra tất cả Invoice trong database
                var allInvoices = await _context.Invoices
                    .Include(i => i.Session)
                    .ThenInclude(s => s.Table)
                    .Include(i => i.Cashier)
                    .OrderByDescending(i => i.PaymentTime)
                    .Take(50)
                    .ToListAsync();

                var debugData = allInvoices.Select(i => new
                {
                    InvoiceId = i.InvoiceId,
                    SessionId = i.SessionId,
                    TotalAmount = i.TotalAmount,
                    PaymentTime = i.PaymentTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                    Status = i.Status,
                    PaymentMethod = i.PaymentMethod,
                    TableName = i.Session?.Table?.TableName ?? "N/A",
                    CashierName = i.Cashier?.FullName ?? "N/A"
                }).ToList();

                // Thống kê nhanh
                var completedCount = allInvoices.Count(i => i.Status == "COMPLETED");
                var totalCompletedAmount = allInvoices
                    .Where(i => i.Status == "COMPLETED" && i.TotalAmount.HasValue)
                    .Sum(i => i.TotalAmount.Value);

                // Kiểm tra range ngày
                var minDate = allInvoices.Where(i => i.PaymentTime.HasValue).Min(i => i.PaymentTime);
                var maxDate = allInvoices.Where(i => i.PaymentTime.HasValue).Max(i => i.PaymentTime);

                return Json(new
                {
                    success = true,
                    totalInvoices = allInvoices.Count,
                    completedInvoices = completedCount,
                    totalCompletedAmount = totalCompletedAmount,
                    dateRange = $"{minDate:yyyy-MM-dd} to {maxDate:yyyy-MM-dd}",
                    invoices = debugData
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Permissions()
        {
            // Check if user is manager
            var role = HttpContext.Session.GetString("Role")?.ToUpper();
            if (role != "MANAGER")
            {
                return RedirectToAction("Login", "Account");
            }

            // Get employees (excluding managers)
            var employees = await _context.Employees
                .Where(e => e.Position != "MANAGER")
                .OrderBy(e => e.FullName)
                .ToListAsync();

            // Get all permissions
            var permissions = await _context.Permissions
                .Where(p => p.IsActive)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.DisplayName)
                .ToListAsync();

            // Group permissions by category
            var permissionsByCategory = permissions
                .GroupBy(p => p.Category)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Get employee permissions
            var employeePermissions = await _context.EmployeePermissions
                .Include(ep => ep.Employee)
                .Include(ep => ep.Permission)
                .Include(ep => ep.GrantedByEmployee)
                .Where(ep => ep.IsActive && ep.Employee.Position != "MANAGER")
                .Select(ep => new EmployeePermissionDto
                {
                    EmployeeId = ep.EmployeeId,
                    EmployeeName = ep.Employee.FullName ?? string.Empty,
                    Position = ep.Employee.Position ?? string.Empty,
                    PermissionId = ep.PermissionId,
                    PermissionName = ep.Permission.PermissionName,
                    PermissionDisplayName = ep.Permission.DisplayName,
                    Category = ep.Permission.Category,
                    GrantedAt = ep.GrantedAt,
                    GrantedByName = ep.GrantedByEmployee != null ? ep.GrantedByEmployee.FullName : null,
                    IsActive = ep.IsActive
                })
                .ToListAsync();

            var viewModel = new PermissionManagementViewModel
            {
                Employees = employees,
                Permissions = permissions,
                PermissionsByCategory = permissionsByCategory,
                EmployeePermissions = employeePermissions
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> GrantPermissions(int employeeId, List<int> permissionIds)
        {
            try
            {
                // Check if user is manager
                var role = HttpContext.Session.GetString("Role")?.ToUpper();
                if (role != "MANAGER")
                {
                    return Json(new { success = false, message = "Không có quyền thực hiện thao tác này" });
                }

                // Get manager ID
                var username = HttpContext.Session.GetString("Username");
                Employee? manager = null;
                
                if (!string.IsNullOrEmpty(username))
                {
                    manager = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Username == username && e.Position == "MANAGER");
                }
                
                // Fallback: if username not in session or manager not found, get manager by EmployeeId
                if (manager == null)
                {
                    var managerId = HttpContext.Session.GetInt32("EmployeeId");
                    if (managerId.HasValue)
                    {
                        manager = await _context.Employees
                            .FirstOrDefaultAsync(e => e.EmployeeId == managerId.Value && e.Position == "MANAGER");
                    }
                }
                
                // Final fallback: get the first available manager
                if (manager == null)
                {
                    manager = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Position == "MANAGER");
                }

                if (manager == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin quản lý. Vui lòng đăng nhập lại." });
                }

                // Check if employee exists and is not a manager
                var employee = await _context.Employees.FindAsync(employeeId);
                if (employee == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy nhân viên" });
                }
                
                if (employee.Position == "MANAGER")
                {
                    return Json(new { success = false, message = "Không thể cấp quyền cho quản lý" });
                }

                // Validate permission IDs
                var validPermissionIds = await _context.Permissions
                    .Where(p => permissionIds.Contains(p.PermissionId) && p.IsActive)
                    .Select(p => p.PermissionId)
                    .ToListAsync();

                if (validPermissionIds.Count != permissionIds.Count)
                {
                    return Json(new { success = false, message = "Một số quyền không hợp lệ hoặc không còn hoạt động" });
                }

                // Remove all existing permissions for this employee (deactivate)
                var existingPermissions = await _context.EmployeePermissions
                    .Where(ep => ep.EmployeeId == employeeId && ep.IsActive)
                    .ToListAsync();

                foreach (var existingPermission in existingPermissions)
                {
                    existingPermission.IsActive = false;
                }

                // Add new permissions
                if (permissionIds.Any())
                {
                    var newEmployeePermissions = permissionIds.Select(permissionId => new EmployeePermission
                    {
                        EmployeeId = employeeId,
                        PermissionId = permissionId,
                        GrantedBy = manager.EmployeeId,
                        GrantedAt = DateTime.Now,
                        IsActive = true
                    }).ToList();

                    _context.EmployeePermissions.AddRange(newEmployeePermissions);
                }

                await _context.SaveChangesAsync();

                // Get permission names for confirmation message
                var permissionNames = await _context.Permissions
                    .Where(p => permissionIds.Contains(p.PermissionId))
                    .Select(p => p.DisplayName)
                    .ToListAsync();

                var message = permissionIds.Any() 
                    ? $"Đã cấp {permissionIds.Count} quyền cho nhân viên {employee.FullName}: {string.Join(", ", permissionNames)}"
                    : $"Đã thu hồi tất cả quyền của nhân viên {employee.FullName}";

                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RevokePermission(int employeeId, int permissionId)
        {
            // Check if user is manager
            var role = HttpContext.Session.GetString("Role")?.ToUpper();
            if (role != "MANAGER")
            {
                return Json(new { success = false, message = "Không có quyền thực hiện thao tác này" });
            }

            var employeePermission = await _context.EmployeePermissions
                .FirstOrDefaultAsync(ep => ep.EmployeeId == employeeId && 
                                         ep.PermissionId == permissionId && 
                                         ep.IsActive);

            if (employeePermission == null)
            {
                return Json(new { success = false, message = "Không tìm thấy quyền này" });
            }

            // Check if employee is not a manager
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null || employee.Position == "MANAGER")
            {
                return Json(new { success = false, message = "Không thể thu hồi quyền của quản lý" });
            }

            employeePermission.IsActive = false;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Thu hồi quyền thành công" });
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeePermissions(int employeeId)
        {
            var permissions = await _context.EmployeePermissions
                .Include(ep => ep.Permission)
                .Where(ep => ep.EmployeeId == employeeId && ep.IsActive)
                .Select(ep => new {
                    permissionId = ep.PermissionId,
                    permissionName = ep.Permission.PermissionName,
                    displayName = ep.Permission.DisplayName,
                    category = ep.Permission.Category
                })
                .ToListAsync();

            return Json(permissions);
        }

        [HttpPost]
        public async Task<IActionResult> InitializePermissions()
        {
            // Check if permissions already exist
            if (await _context.Permissions.AnyAsync())
            {
                return Json(new { success = false, message = "Quyền đã được khởi tạo" });
            }

            var permissions = new List<Permission>
            {
                // Quản lý bàn
                new Permission { PermissionName = "TABLE_VIEW", DisplayName = "Xem thông tin bàn", Category = "Quản lý bàn", Description = "Có thể xem danh sách và trạng thái các bàn" },
                new Permission { PermissionName = "TABLE_MANAGE", DisplayName = "Quản lý bàn", Category = "Quản lý bàn", Description = "Có thể thêm, sửa, xóa bàn và thay đổi trạng thái bàn" },
                new Permission { PermissionName = "TABLE_TRANSFER", DisplayName = "Chuyển bàn", Category = "Quản lý bàn", Description = "Có thể chuyển phiên chơi từ bàn này sang bàn khác" },
                
                // Quản lý sản phẩm
                new Permission { PermissionName = "PRODUCT_VIEW", DisplayName = "Xem sản phẩm", Category = "Quản lý sản phẩm", Description = "Có thể xem danh sách sản phẩm" },
                new Permission { PermissionName = "PRODUCT_MANAGE", DisplayName = "Quản lý sản phẩm", Category = "Quản lý sản phẩm", Description = "Có thể thêm, sửa, xóa sản phẩm" },
                
                // Đặt bàn
                new Permission { PermissionName = "BOOKING_VIEW", DisplayName = "Xem đặt bàn", Category = "Đặt bàn", Description = "Có thể xem danh sách đặt bàn" },
                new Permission { PermissionName = "BOOKING_CREATE", DisplayName = "Tạo đặt bàn", Category = "Đặt bàn", Description = "Có thể tạo đặt bàn mới" },
                new Permission { PermissionName = "BOOKING_CANCEL", DisplayName = "Hủy đặt bàn", Category = "Đặt bàn", Description = "Có thể hủy đặt bàn" },
                
                // Phiên chơi
                new Permission { PermissionName = "SESSION_START", DisplayName = "Bắt đầu phiên", Category = "Phiên chơi", Description = "Có thể bắt đầu phiên chơi" },
                new Permission { PermissionName = "SESSION_END", DisplayName = "Kết thúc phiên", Category = "Phiên chơi", Description = "Có thể kết thúc phiên chơi" },
                
                // Dịch vụ bàn
                new Permission { PermissionName = "ORDER_VIEW", DisplayName = "Xem đơn hàng", Category = "Dịch vụ bàn", Description = "Có thể xem đơn hàng của bàn" },
                new Permission { PermissionName = "ORDER_MANAGE", DisplayName = "Quản lý đơn hàng", Category = "Dịch vụ bàn", Description = "Có thể thêm, sửa, xóa đơn hàng" },
                
                // Thanh toán
                new Permission { PermissionName = "PAYMENT_PROCESS", DisplayName = "Xử lý thanh toán", Category = "Thanh toán", Description = "Có thể thực hiện thanh toán hóa đơn cho các bàn" },
                
                // Báo cáo
                new Permission { PermissionName = "REVENUE_VIEW", DisplayName = "Xem báo cáo doanh thu", Category = "Báo cáo", Description = "Có thể xem báo cáo doanh thu" },
                new Permission { PermissionName = "REVENUE_EXPORT", DisplayName = "Xuất báo cáo", Category = "Báo cáo", Description = "Có thể xuất báo cáo doanh thu" },
                
                // Khách hàng
                new Permission { PermissionName = "CUSTOMER_VIEW", DisplayName = "Xem khách hàng", Category = "Khách hàng", Description = "Có thể xem thông tin khách hàng" },
                new Permission { PermissionName = "CUSTOMER_MANAGE", DisplayName = "Quản lý khách hàng", Category = "Khách hàng", Description = "Có thể thêm, sửa thông tin khách hàng" }
            };

            _context.Permissions.AddRange(permissions);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Khởi tạo quyền thành công" });
        }

        [HttpGet]
        public async Task<IActionResult> GetTemplatePermissions(string templateType)
        {
            try
            {
                var permissions = templateType.ToUpper() switch
                {
                    "CASHIER" => new[] { "TABLE_VIEW", "REVENUE_VIEW", "BOOKING_VIEW", "PRODUCT_VIEW", "PAYMENT_PROCESS", "TABLE_TRANSFER" },
                    "SERVING" => new[] { "TABLE_VIEW", "BOOKING_CREATE", "SESSION_START", "ORDER_MANAGE", "PRODUCT_VIEW", "PAYMENT_PROCESS", "TABLE_TRANSFER" },
                    _ => new string[0]
                };

                var permissionIds = await _context.Permissions
                    .Where(p => permissions.Contains(p.PermissionName) && p.IsActive)
                    .Select(p => new { p.PermissionId, p.PermissionName, p.DisplayName })
                    .ToListAsync();

                return Json(new { success = true, data = permissionIds });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApplyBatchPermissions(List<int> employeeIds, string templateType)
        {
            try
            {
                // Check if user is manager
                var role = HttpContext.Session.GetString("Role")?.ToUpper();
                if (role != "MANAGER")
                {
                    return Json(new { success = false, message = "Không có quyền thực hiện thao tác này" });
                }

                if (!employeeIds.Any())
                {
                    return Json(new { success = false, message = "Vui lòng chọn ít nhất một nhân viên" });
                }

                // Get manager ID
                var username = HttpContext.Session.GetString("Username");
                Employee? manager = null;
                
                if (!string.IsNullOrEmpty(username))
                {
                    manager = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Username == username && e.Position == "MANAGER");
                }
                
                // Fallback: if username not in session or manager not found, get manager by EmployeeId
                if (manager == null)
                {
                    var managerId = HttpContext.Session.GetInt32("EmployeeId");
                    if (managerId.HasValue)
                    {
                        manager = await _context.Employees
                            .FirstOrDefaultAsync(e => e.EmployeeId == managerId.Value && e.Position == "MANAGER");
                    }
                }
                
                // Final fallback: get the first available manager
                if (manager == null)
                {
                    manager = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Position == "MANAGER");
                }

                if (manager == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin quản lý. Vui lòng đăng nhập lại." });
                }

                // Get template permissions
                var templatePermissions = templateType.ToUpper() switch
                {
                    "CASHIER" => new[] { "TABLE_VIEW", "REVENUE_VIEW", "BOOKING_VIEW", "PRODUCT_VIEW", "PAYMENT_PROCESS", "TABLE_TRANSFER" },
                    "SERVING" => new[] { "TABLE_VIEW", "BOOKING_CREATE", "SESSION_START", "ORDER_MANAGE", "PRODUCT_VIEW", "PAYMENT_PROCESS", "TABLE_TRANSFER" },
                    _ => new string[0]
                };

                var permissionIds = await _context.Permissions
                    .Where(p => templatePermissions.Contains(p.PermissionName) && p.IsActive)
                    .Select(p => p.PermissionId)
                    .ToListAsync();

                int successCount = 0;
                var errors = new List<string>();

                foreach (var employeeId in employeeIds)
                {
                    try
                    {
                        // Check if employee exists and is not a manager
                        var employee = await _context.Employees.FindAsync(employeeId);
                        if (employee == null || employee.Position == "MANAGER")
                        {
                            errors.Add($"Nhân viên ID {employeeId} không hợp lệ");
                            continue;
                        }

                        // Remove existing permissions
                        var existingPermissions = await _context.EmployeePermissions
                            .Where(ep => ep.EmployeeId == employeeId && ep.IsActive)
                            .ToListAsync();

                        foreach (var existingPermission in existingPermissions)
                        {
                            existingPermission.IsActive = false;
                        }

                        // Add new permissions
                        if (permissionIds.Any())
                        {
                            var newEmployeePermissions = permissionIds.Select(permissionId => new EmployeePermission
                            {
                                EmployeeId = employeeId,
                                PermissionId = permissionId,
                                GrantedBy = manager.EmployeeId,
                                GrantedAt = DateTime.Now,
                                IsActive = true
                            }).ToList();

                            _context.EmployeePermissions.AddRange(newEmployeePermissions);
                        }

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Lỗi khi xử lý nhân viên ID {employeeId}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                if (errors.Any())
                {
                    return Json(new { 
                        success = true, 
                        message = $"Đã phân quyền thành công cho {successCount} nhân viên. Có {errors.Count} lỗi xảy ra.",
                        errors = errors
                    });
                }

                return Json(new { 
                    success = true, 
                    message = $"Đã phân quyền thành công cho {successCount} nhân viên với mẫu {templateType}" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult BackToDashboard()
        {
            var role = HttpContext.Session.GetString("Role")?.ToUpper();
            
            return role switch
            {
                "MANAGER" => RedirectToAction("Index", "Manager"),
                "CASHIER" => RedirectToAction("Index", "Cashier"),
                "SERVING" => RedirectToAction("Index", "Serving"),
                _ => RedirectToAction("Login", "Account")
            };
        }

        [HttpPost]
        public async Task<IActionResult> CleanupOrphanedOrders()
        {
            try
            {
                var success = await _orderService.CleanupOrphanedOrdersAsync();
                
                if (success)
                {
                    return Json(new { 
                        success = true, 
                        message = "Đã dọn dẹp các đơn hàng không hợp lệ thành công" 
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Có lỗi xảy ra khi dọn dẹp đơn hàng" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Table Transfer functionality
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
        public async Task<IActionResult> DebugServiceCalculation()
        {
            try
            {
                // Get all active sessions with orders
                var activeSessions = await _context.Sessions
                    .Include(s => s.Table)
                    .Include(s => s.Orders)
                        .ThenInclude(o => o.OrderDetails)
                            .ThenInclude(od => od.Product)
                    .Include(s => s.Invoices)
                    .Where(s => s.EndTime == null)
                    .ToListAsync();

                var debugData = new List<object>();

                foreach (var session in activeSessions)
                {
                    var invoice = session.Invoices.FirstOrDefault(i => i.Status == "PENDING");
                    if (invoice != null)
                    {
                        var (tableTotal, serviceTotal, grandTotal) = await _invoiceService.CalculateInvoiceTotalsAsync(session.SessionId);
                        var orderDetails = await _invoiceService.GetInvoiceOrderDetailsAsync(session.SessionId);

                        debugData.Add(new
                        {
                            sessionId = session.SessionId,
                            invoiceId = invoice.InvoiceId,
                            tableName = session.Table?.TableName,
                            tableTotal = tableTotal,
                            serviceTotal = serviceTotal,
                            grandTotal = grandTotal,
                            orderDetailsCount = orderDetails.Count,
                            orderDetails = orderDetails
                        });
                    }
                }

                return Json(new { success = true, data = debugData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}

