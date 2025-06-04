using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly BilliardsDbContext _context;
        private readonly IInvoiceService _invoiceService;

        public TransactionService(BilliardsDbContext context, IInvoiceService invoiceService)
        {
            _context = context;
            _invoiceService = invoiceService;
        }

        public async Task<object> GetRecentTransactionsAsync(int days = 7, int limit = 20)
        {
            var fromDate = DateTime.Now.AddDays(-days).Date;
            var toDate = DateTime.Now.Date.AddDays(1); // Include today fully
            
            var transactions = await GetTransactionsByDateRangeAsync(fromDate, toDate, limit);
            var statistics = await GetTransactionStatisticsAsync(fromDate, toDate);

            var transactionData = new List<object>();
            foreach (var transaction in transactions)
            {
                var tableTotal = await CalculateTableTotalForCompletedSession(transaction.Session);
                var serviceTotal = await CalculateServiceTotalForCompletedSession(transaction.Session);

                transactionData.Add(new
                {
                    invoiceId = transaction.InvoiceId,
                    tableName = transaction.Session?.Table?.TableName ?? "N/A",
                    amount = transaction.TotalAmount ?? 0,
                    tableTotal = tableTotal,
                    serviceTotal = serviceTotal,
                    paymentMethod = transaction.PaymentMethod ?? "CASH",
                    cashierName = transaction.Cashier?.FullName ?? "N/A",
                    paymentTime = transaction.PaymentTime?.ToString("dd/MM/yyyy HH:mm") ?? "",
                    sessionDuration = transaction.Session != null ? FormatSessionDuration(transaction.Session) : "N/A"
                });
            }

            return new
            {
                statistics = statistics,
                transactions = transactionData
            };
        }

        public async Task<List<Invoice>> GetTransactionsByDateRangeAsync(DateTime fromDate, DateTime toDate, int limit = 50)
        {
            return await _context.Invoices
                .Include(i => i.Session)
                .ThenInclude(s => s.Table)
                .Include(i => i.Cashier)
                .Where(i => i.Status == "COMPLETED" && 
                           i.PaymentTime.HasValue && 
                           i.PaymentTime.Value >= fromDate && 
                           i.PaymentTime.Value < toDate)
                .OrderByDescending(i => i.PaymentTime)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<object> GetTransactionStatisticsAsync(DateTime fromDate, DateTime toDate)
        {
            var transactions = await GetTransactionsByDateRangeAsync(fromDate, toDate, int.MaxValue);

            return new
            {
                totalTransactions = transactions.Count,
                totalAmount = transactions.Sum(t => t.TotalAmount ?? 0),
                avgTransactionValue = transactions.Any() ? transactions.Average(t => t.TotalAmount ?? 0) : 0,
                fromDate = fromDate.ToString("dd/MM/yyyy"),
                toDate = DateTime.Now.ToString("dd/MM/yyyy"),
                paymentMethodStats = transactions
                    .GroupBy(t => t.PaymentMethod ?? "CASH")
                    .Select(g => new
                    {
                        method = g.Key,
                        count = g.Count(),
                        amount = g.Sum(t => t.TotalAmount ?? 0)
                    })
                    .ToList()
            };
        }

        private async Task<decimal> CalculateTableTotalForCompletedSession(Session? session)
        {
            if (session == null || !session.StartTime.HasValue || !session.EndTime.HasValue)
                return 0;

            return await _invoiceService.CalculateTableTotalForSession(session, session.EndTime);
        }

        private async Task<decimal> CalculateServiceTotalForCompletedSession(Session? session)
        {
            if (session == null)
                return 0;

            return await _invoiceService.CalculateServiceTotalForSession(session);
        }

        private string FormatSessionDuration(Session session)
        {
            if (!session.StartTime.HasValue || !session.EndTime.HasValue)
                return "N/A";

            var duration = session.EndTime.Value - session.StartTime.Value;
            return $"{duration.Hours:00}:{duration.Minutes:00}";
        }
    }
} 