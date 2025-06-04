using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Services
{
    public interface IInvoiceService
    {
        Task<(decimal TableTotal, decimal ServiceTotal, decimal GrandTotal)> CalculateInvoiceTotalsAsync(int sessionId);
        Task<(decimal TableTotal, decimal ServiceTotal, decimal GrandTotal)> CalculateInvoiceTotalsByInvoiceIdAsync(int invoiceId);
        Task<List<dynamic>> GetInvoiceOrderDetailsAsync(int sessionId);
        Task<List<dynamic>> GetInvoiceOrderDetailsByInvoiceIdAsync(int invoiceId);
        Task<bool> UpdateInvoiceTotalsAsync(int invoiceId);
        Task<decimal> CalculateTableTotalForSession(Session session, DateTime? endTime = null);
        Task<decimal> CalculateServiceTotalForSession(Session session);
        Task<object> GetActiveSessionsDebugInfoAsync();
        Task<(bool Success, string Message, int CreatedCount)> CreateMissingInvoicesAsync();
    }
} 