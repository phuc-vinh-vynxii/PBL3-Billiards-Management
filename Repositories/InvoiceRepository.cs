using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly BilliardsDbContext _context;

        public InvoiceRepository(BilliardsDbContext context)
        {
            _context = context;
        }

        public async Task<Invoice?> GetByIdAsync(int id)
        {
            return await _context.Invoices.FindAsync(id);
        }

        public async Task<Invoice?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Invoices
                .Include(i => i.Session)
                .ThenInclude(s => s.Table)
                .Include(i => i.Session)
                .ThenInclude(s => s.Orders)
                .ThenInclude(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(i => i.Cashier)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);
        }

        public async Task<Invoice?> GetBySessionIdAsync(int sessionId)
        {
            return await _context.Invoices
                .FirstOrDefaultAsync(i => i.SessionId == sessionId);
        }

        public async Task<List<Invoice>> GetAllAsync()
        {
            return await _context.Invoices
                .Include(i => i.Session)
                .ThenInclude(s => s.Table)
                .Include(i => i.Cashier)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetByStatusAsync(string status)
        {
            return await _context.Invoices
                .Include(i => i.Session)
                .ThenInclude(s => s.Table)
                .Include(i => i.Cashier)
                .Where(i => i.Status == status)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetPendingWithActiveSessionsAsync()
        {
            return await _context.Invoices
                .Include(i => i.Session)
                .ThenInclude(s => s.Table)
                .Where(i => i.Status == "PENDING" && 
                           i.Session != null && 
                           i.Session.EndTime == null)
                .OrderBy(i => i.Session.StartTime)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.Invoices
                .Include(i => i.Session)
                .ThenInclude(s => s.Table)
                .Include(i => i.Cashier)
                .Where(i => i.PaymentTime.HasValue && 
                           i.PaymentTime.Value.Date >= fromDate.Date && 
                           i.PaymentTime.Value.Date <= toDate.Date &&
                           i.Status == "COMPLETED")
                .OrderByDescending(i => i.PaymentTime)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetTodayInvoicesAsync()
        {
            var today = DateTime.Today;
            return await _context.Invoices
                .Where(i => i.PaymentTime.HasValue && 
                           i.PaymentTime.Value.Date == today && 
                           i.Status == "COMPLETED")
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetRecentInvoicesAsync(int count = 50)
        {
            return await _context.Invoices
                .Include(i => i.Session)
                .ThenInclude(s => s.Table)
                .Include(i => i.Cashier)
                .OrderByDescending(i => i.PaymentTime)
                .Take(count)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalRevenueByDateAsync(DateTime date)
        {
            return await _context.Invoices
                .Where(i => i.PaymentTime.HasValue && 
                           i.PaymentTime.Value.Date == date.Date && 
                           i.Status == "COMPLETED")
                .SumAsync(i => i.TotalAmount ?? 0);
        }

        public async Task<decimal> GetTotalRevenueByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.Invoices
                .Where(i => i.PaymentTime.HasValue && 
                           i.PaymentTime.Value.Date >= fromDate.Date && 
                           i.PaymentTime.Value.Date <= toDate.Date &&
                           i.Status == "COMPLETED")
                .SumAsync(i => i.TotalAmount ?? 0);
        }

        public async Task<int> GetCountByDateAsync(DateTime date)
        {
            return await _context.Invoices
                .CountAsync(i => i.PaymentTime.HasValue && 
                           i.PaymentTime.Value.Date == date.Date && 
                           i.Status == "COMPLETED");
        }

        public async Task<Invoice> CreateAsync(Invoice invoice)
        {
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }

        public async Task<Invoice> UpdateAsync(Invoice invoice)
        {
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }

        public async Task DeleteAsync(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
} 