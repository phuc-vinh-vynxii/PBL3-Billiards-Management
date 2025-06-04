using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Services
{
    public class SessionService : ISessionService
    {
        private readonly BilliardsDbContext _context;

        public SessionService(BilliardsDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, Session? Session)> StartSessionAsync(int tableId, int employeeId, int? customerId = null, int? reservationId = null)
        {
            try
            {
                // Check if table is already in use
                if (await IsTableInUseAsync(tableId))
                {
                    return (false, "Bàn đang được sử dụng", null);
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

                // Create a pending invoice for this session
                var invoice = new Invoice
                {
                    SessionId = session.SessionId,
                    CashierId = null,
                    TotalAmount = 0,
                    PaymentTime = null,
                    PaymentMethod = null,
                    Status = "PENDING",
                    Discount = 0
                };

                _context.Invoices.Add(invoice);

                // Update reservation status if applicable
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

                return (true, "Bắt đầu sử dụng bàn thành công", session);
            }
            catch (Exception ex)
            {
                return (false, $"Có lỗi xảy ra: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> EndSessionAsync(int sessionId)
        {
            try
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

                // Calculate total usage time accurately
                if (session.StartTime.HasValue)
                {
                    var duration = now - session.StartTime.Value;
                    double totalHoursExact = duration.TotalHours;
                    session.TotalTime = (int)(duration.TotalMinutes);

                    // Calculate table total based on exact usage time
                    var pricePerHour = session.Table?.PricePerHour ?? 0;
                    session.TableTotal = (decimal)(totalHoursExact * (double)pricePerHour);
                }
                else
                {
                    session.TotalTime = 0;
                    session.TableTotal = 0;
                }

                // Calculate service total and complete orders
                decimal orderTotal = 0;
                
                // Tính tổng từ OrderDetails thay vì từ Orders
                var orderDetails = await _context.OrderDetails
                    .Include(od => od.Order)
                    .Where(od => od.Order != null && 
                               od.Order.SessionId == sessionId &&
                               od.UnitPrice.HasValue && 
                               od.Quantity.HasValue)
                    .ToListAsync();

                orderTotal = orderDetails.Sum(od => od.UnitPrice.Value * od.Quantity.Value);

                // Complete all orders for this session
                var ordersToComplete = await _context.Orders
                    .Where(o => o.SessionId == sessionId && o.Status == "PENDING")
                    .ToListAsync();

                foreach (var order in ordersToComplete)
                {
                    order.Status = "COMPLETED";
                }

                // Update table status
                if (session.Table != null)
                {
                    session.Table.Status = "AVAILABLE";
                }

                await _context.SaveChangesAsync();

                return (true, "Kết thúc phiên sử dụng bàn thành công");
            }
            catch (Exception ex)
            {
                return (false, $"Có lỗi xảy ra: {ex.Message}");
            }
        }

        public async Task<Session?> GetActiveSessionByTableIdAsync(int tableId)
        {
            return await _context.Sessions
                .Include(s => s.Table)
                .Include(s => s.Orders)
                .ThenInclude(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(s => s.TableId == tableId && s.EndTime == null);
        }

        public async Task<Session?> GetSessionByIdAsync(int sessionId)
        {
            return await _context.Sessions
                .Include(s => s.Table)
                .Include(s => s.Orders)
                .ThenInclude(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        }

        public async Task<List<Session>> GetActiveSessionsAsync()
        {
            return await _context.Sessions
                .Include(s => s.Table)
                .Where(s => s.EndTime == null)
                .ToListAsync();
        }

        public async Task<bool> IsTableInUseAsync(int tableId)
        {
            return await _context.Sessions
                .AnyAsync(s => s.TableId == tableId && s.EndTime == null);
        }

        public async Task<int?> ResolveEmployeeIdAsync(int? employeeId, string? username, string? role)
        {
            if (employeeId.HasValue)
                return employeeId.Value;

            if (role?.ToUpper() == "MANAGER")
            {
                if (!string.IsNullOrEmpty(username))
                {
                    var manager = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Username == username && e.Position == "MANAGER");

                    if (manager != null)
                        return manager.EmployeeId;
                }

                // Fallback to first manager
                var firstManager = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Position == "MANAGER");

                if (firstManager != null)
                    return firstManager.EmployeeId;

                // Last resort: first employee
                var firstEmployee = await _context.Employees.FirstOrDefaultAsync();
                return firstEmployee?.EmployeeId;
            }

            return null;
        }
    }
} 