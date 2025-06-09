using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly BilliardsDbContext _context;

        public InvoiceService(BilliardsDbContext context)
        {
            _context = context;
        }

        public async Task<(decimal TableTotal, decimal ServiceTotal, decimal GrandTotal)> CalculateInvoiceTotalsAsync(int sessionId)
        {
            var session = await _context.Sessions
                .Include(s => s.Table)
                .Include(s => s.Orders)
                    .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return (0, 0, 0);

            var tableTotal = await CalculateTableTotalForSession(session);
            var serviceTotal = await CalculateServiceTotalForSession(session);
            var grandTotal = tableTotal + serviceTotal;

            return (tableTotal, serviceTotal, grandTotal);
        }

        public async Task<(decimal TableTotal, decimal ServiceTotal, decimal GrandTotal)> CalculateInvoiceTotalsByInvoiceIdAsync(int invoiceId)
        {
            try
            {
                Console.WriteLine($"DEBUG InvoiceService: CalculateInvoiceTotalsByInvoiceIdAsync called with invoiceId: {invoiceId}");
                
                var invoice = await _context.Invoices
                    .Include(i => i.Session)
                        .ThenInclude(s => s.Table)
                    .Include(i => i.Session)
                        .ThenInclude(s => s.Orders)
                            .ThenInclude(o => o.OrderDetails)
                    .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

                if (invoice?.Session == null)
                {
                    Console.WriteLine($"DEBUG InvoiceService: Invoice or Session is null for invoiceId: {invoiceId}");
                    return (0, 0, 0);
                }

                Console.WriteLine($"DEBUG InvoiceService: Found session {invoice.Session.SessionId} for invoice {invoiceId}");

                var tableTotal = await CalculateTableTotalForSession(invoice.Session);
                Console.WriteLine($"DEBUG InvoiceService: Table total: {tableTotal}");
                
                var serviceTotal = await CalculateServiceTotalForSession(invoice.Session);
                Console.WriteLine($"DEBUG InvoiceService: Service total: {serviceTotal}");
                
                var grandTotal = tableTotal + serviceTotal;
                Console.WriteLine($"DEBUG InvoiceService: Grand total: {grandTotal}");

                return (tableTotal, serviceTotal, grandTotal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG InvoiceService ERROR in CalculateInvoiceTotalsByInvoiceIdAsync: {ex.Message}");
                Console.WriteLine($"DEBUG InvoiceService ERROR Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<List<dynamic>> GetInvoiceOrderDetailsAsync(int sessionId)
        {
            var orderDetails = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => od.Order != null && 
                           od.Order.SessionId == sessionId && 
                           od.Product != null)
                .Select(od => new {
                    productName = od.Product.ProductName,
                    productType = od.Product.ProductType.ToString(),
                    quantity = od.Quantity ?? 0,
                    unitPrice = od.UnitPrice ?? 0,
                    total = (od.Quantity ?? 0) * (od.UnitPrice ?? 0)
                })
                .ToListAsync();

            return orderDetails.Cast<dynamic>().ToList();
        }

        public async Task<List<dynamic>> GetInvoiceOrderDetailsByInvoiceIdAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice?.SessionId == null)
                return new List<dynamic>();

            return await GetInvoiceOrderDetailsAsync(invoice.SessionId.Value);
        }

        public async Task<bool> UpdateInvoiceTotalsAsync(int invoiceId)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Session)
                        .ThenInclude(s => s.Table)
                    .Include(i => i.Session)
                        .ThenInclude(s => s.Orders)
                            .ThenInclude(o => o.OrderDetails)
                    .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

                if (invoice?.Session == null)
                    return false;

                var (tableTotal, serviceTotal, grandTotal) = await CalculateInvoiceTotalsByInvoiceIdAsync(invoiceId);

                // Update session totals if session hasn't ended
                if (invoice.Session.EndTime == null)
                {
                    invoice.Session.TableTotal = tableTotal;
                }

                // Update invoice total
                invoice.TotalAmount = grandTotal;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<decimal> CalculateTableTotalForSession(Session session, DateTime? endTime = null)
        {
            if (session?.Table == null || !session.StartTime.HasValue)
                return 0;

            var calculationEndTime = endTime ?? (session.EndTime ?? DateTime.Now);
            var duration = calculationEndTime - session.StartTime.Value;
            var pricePerHour = session.Table.PricePerHour ?? 0;

            return (decimal)(duration.TotalHours * (double)pricePerHour);
        }

        public async Task<decimal> CalculateServiceTotalForSession(Session session)
        {
            if (session?.Orders == null)
                return 0;

            // Tính tổng từ tất cả OrderDetails của session này
            var serviceTotal = await _context.OrderDetails
                .Include(od => od.Order)
                .Where(od => od.Order != null && 
                           od.Order.SessionId == session.SessionId &&
                           od.UnitPrice.HasValue && 
                           od.Quantity.HasValue)
                .SumAsync(od => od.UnitPrice.Value * od.Quantity.Value);

            return serviceTotal;
        }

        public async Task<object> GetActiveSessionsDebugInfoAsync()
        {
            // Get all active sessions (EndTime = null)
            var activeSessions = await _context.Sessions
                .Include(s => s.Table)
                .Include(s => s.Invoices)
                .Where(s => s.EndTime == null)
                .ToListAsync();

            var sessionData = activeSessions.Select(session => new
            {
                sessionId = session.SessionId,
                tableId = session.TableId,
                tableName = session.Table?.TableName ?? "N/A",
                startTime = session.StartTime?.ToString("HH:mm dd/MM/yyyy") ?? "N/A",
                hasInvoices = session.Invoices?.Count ?? 0,
                invoices = session.Invoices?.Select(inv => new
                {
                    invoiceId = inv.InvoiceId,
                    status = inv.Status,
                    totalAmount = inv.TotalAmount,
                    paymentTime = inv.PaymentTime?.ToString("HH:mm dd/MM/yyyy")
                }).Cast<object>().ToList() ?? new List<object>()
            }).ToList();

            return new { 
                success = true, 
                message = $"Found {activeSessions.Count} active sessions",
                data = sessionData 
            };
        }

        public async Task<(bool Success, string Message, int CreatedCount)> CreateMissingInvoicesAsync()
        {
            try
            {
                // Find active sessions without invoices
                var sessionsWithoutInvoices = await _context.Sessions
                    .Include(s => s.Invoices)
                    .Include(s => s.Table)
                    .Where(s => s.EndTime == null && !s.Invoices.Any())
                    .ToListAsync();

                var createdCount = 0;
                foreach (var session in sessionsWithoutInvoices)
                {
                    var invoice = new Invoice
                    {
                        SessionId = session.SessionId,
                        CashierId = null, // Will be set when payment is processed
                        TotalAmount = 0, // Will be calculated when payment is processed
                        PaymentTime = null,
                        PaymentMethod = null,
                        Status = "PENDING"
                    };

                    _context.Invoices.Add(invoice);
                    createdCount++;
                }

                await _context.SaveChangesAsync();

                return (true, $"Created {createdCount} missing invoices for active sessions", createdCount);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, 0);
            }
        }
    }
} 