using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Repositories
{
    public interface IInvoiceRepository
    {
        Task<Invoice?> GetByIdAsync(int id);
        Task<Invoice?> GetByIdWithDetailsAsync(int id);
        Task<Invoice?> GetBySessionIdAsync(int sessionId);
        Task<List<Invoice>> GetAllAsync();
        Task<List<Invoice>> GetByStatusAsync(string status);
        Task<List<Invoice>> GetPendingWithActiveSessionsAsync();
        Task<List<Invoice>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);
        Task<List<Invoice>> GetTodayInvoicesAsync();
        Task<List<Invoice>> GetRecentInvoicesAsync(int count = 50);
        Task<decimal> GetTotalRevenueByDateAsync(DateTime date);
        Task<decimal> GetTotalRevenueByDateRangeAsync(DateTime fromDate, DateTime toDate);
        Task<int> GetCountByDateAsync(DateTime date);
        Task<Invoice> CreateAsync(Invoice invoice);
        Task<Invoice> UpdateAsync(Invoice invoice);
        Task DeleteAsync(int id);
        Task SaveChangesAsync();
    }
} 