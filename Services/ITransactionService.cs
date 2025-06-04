using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Services
{
    public interface ITransactionService
    {
        Task<object> GetRecentTransactionsAsync(int days = 7, int limit = 20);
        Task<List<Invoice>> GetTransactionsByDateRangeAsync(DateTime fromDate, DateTime toDate, int limit = 50);
        Task<object> GetTransactionStatisticsAsync(DateTime fromDate, DateTime toDate);
    }
} 