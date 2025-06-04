using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;

namespace BilliardsManagement.Services
{
    public class BookingService : IBookingService
    {
        private readonly BilliardsDbContext _context;

        public BookingService(BilliardsDbContext context)
        {
            _context = context;
        }

        public async Task<BookingViewModel> GetBookingDataAsync()
        {
            // Update expired reservations first
            await UpdateExpiredReservationsAsync();
            
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
            
            return new BookingViewModel
            {
                Tables = tables,
                Customers = customers,
                Reservations = reservations
            };
        }

        public async Task UpdateExpiredReservationsAsync()
        {
            try
            {
                var now = DateTime.Now;
                
                // Get all expired reservations that are still CONFIRMED
                var expiredReservations = await _context.Reservations
                    .Where(r => r.EndTime <= now && r.Status == "CONFIRMED")
                    .ToListAsync();
                
                if (expiredReservations.Any())
                {
                    foreach (var reservation in expiredReservations)
                    {
                        // Update status to EXPIRED
                        reservation.Status = "EXPIRED";
                    }
                    
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                // Log error in production
            }
        }

        public async Task<(bool Success, string Message)> CreateReservationAsync(int tableId, int customerId, string bookingDate, string startTimeStr, string endTimeStr, string notes)
        {
            // Check for null or empty values
            if (string.IsNullOrEmpty(bookingDate) || string.IsNullOrEmpty(startTimeStr) || string.IsNullOrEmpty(endTimeStr))
            {
                return (false, "Thời gian bắt đầu và kết thúc không được để trống");
            }
            
            // Verify that the customer exists
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                return (false, "Khách hàng không tồn tại. Vui lòng chọn khách hàng khác.");
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
            catch (Exception)
            {
                return (false, "Lỗi định dạng ngày giờ");
            }
            
            // Ensure dates are valid
            if (startDateTime >= endDateTime)
            {
                return (false, "Thời gian kết thúc phải sau thời gian bắt đầu");
            }

            // Make sure start time is not in the past
            if (startDateTime < DateTime.Now)
            {
                return (false, "Không thể đặt bàn trong quá khứ");
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
                return (false, "Bàn đã được đặt trong khoảng thời gian này");
            }

            // Check if the table exists
            var table = await _context.Tables.FindAsync(tableId);
            if (table == null)
            {
                return (false, "Bàn không tồn tại");
            }
            
            // If table is in use, check if reservation is for the future (not conflicting with current session)
            if (table.Status == "IN_USE" && startDateTime <= DateTime.Now)
            {
                return (false, "Bàn đang được sử dụng, chỉ có thể đặt bàn cho thời gian trong tương lai");
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
            
            return (true, "Đặt bàn thành công");
        }

        public async Task<(bool Success, string Message)> CancelReservationAsync(int reservationId)
        {
            var reservation = await _context.Reservations.FindAsync(reservationId);
            if (reservation == null)
                return (false, "Không tìm thấy đơn đặt bàn");
                
            reservation.Status = "CANCELLED";
            await _context.SaveChangesAsync();
            
            return (true, "Hủy đặt bàn thành công");
        }

        public async Task<(bool Success, string Message)> StartSessionAsync(int tableId, int employeeId, int? customerId = null, int? reservationId = null)
        {
            // Check if table is already in use
            var isTableInUse = await _context.Sessions
                .AnyAsync(s => s.TableId == tableId && s.EndTime == null);
                
            if (isTableInUse)
            {
                return (false, "Bàn đang được sử dụng");
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
            await _context.SaveChangesAsync();
            
            // Create a pending invoice for this session (IMPORTANT - for cashier to see)
            var invoice = new Invoice
            {
                SessionId = session.SessionId,
                CashierId = null, // Will be set when payment is processed
                TotalAmount = 0, // Will be calculated when payment is processed
                PaymentTime = null, // Will be set when payment is processed
                PaymentMethod = null, // Will be set when payment is processed
                Status = "PENDING", // IMPORTANT: Set as PENDING so cashier can see it
                Discount = 0
            };
            
            _context.Invoices.Add(invoice);
            
            // If this is from a reservation, update the reservation status
            if (reservationId.HasValue)
            {
                var reservation = await _context.Reservations.FindAsync(reservationId.Value);
                if (reservation != null)
                {
                    reservation.Status = "USED";
                }
            }
            
            // Update table status
            var table = await _context.Tables.FindAsync(tableId);
            if (table != null)
            {
                table.Status = "OCCUPIED";
            }
            
            await _context.SaveChangesAsync();
            
            return (true, "Bắt đầu sử dụng bàn thành công");
        }

        public async Task<(bool Success, string Message)> EndSessionAsync(int sessionId)
        {
            // Find current session
            var session = await _context.Sessions
                .Include(s => s.Table)
                .Include(s => s.Orders)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.EndTime == null);
                
            if (session == null)
            {
                return (false, "Không tìm thấy phiên sử dụng hoặc phiên đã kết thúc");
            }
            
            // End session
            var now = DateTime.Now;
            session.EndTime = now;
            
            // Calculate total usage time (calculated accurately in hours)
            if (session.StartTime.HasValue)
            {
                var duration = now - session.StartTime.Value;
                
                // Save exact playtime (calculated in hours with decimal)
                double totalHoursExact = duration.TotalHours;
                session.TotalTime = (int)(duration.TotalMinutes); // Still save total minutes for backward compatibility
                
                // Calculate total amount based on table type and EXACT usage time
                var pricePerHour = session.Table?.PricePerHour ?? 0;
                session.TableTotal = (decimal)(totalHoursExact * (double)pricePerHour);
            }
            else
            {
                session.TotalTime = 0;
                session.TableTotal = 0;
            }
            
            // Calculate total service amount
            decimal orderTotal = 0;
            foreach (var order in session.Orders)
            {
                foreach (var detail in order.OrderDetails)
                {
                    orderTotal += (detail.UnitPrice ?? 0) * (detail.Quantity ?? 0);
                }
                
                // Update order status
                order.Status = "COMPLETED";
            }
            
            // Update table status
            if (session.Table != null)
            {
                session.Table.Status = "AVAILABLE";
            }
            
            await _context.SaveChangesAsync();
            
            return (true, "Kết thúc sử dụng bàn thành công");
        }

        public async Task<List<dynamic>> GetUpcomingReservationsAsync()
        {
            // Update expired reservations first
            await UpdateExpiredReservationsAsync();
            
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
            }).Cast<dynamic>().ToList();

            return result;
        }
    }
} 