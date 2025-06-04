using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;
using System.Globalization;

namespace BilliardsManagement.Services
{
    public interface IRevenueService
    {
        Task<RevenueViewModel> GetRevenueDataAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<DailyRevenueData>> GetDailyRevenueAsync(DateTime fromDate, DateTime toDate);
        Task<List<MonthlyRevenueData>> GetMonthlyRevenueAsync(int year);
        Task<List<ProductRevenueData>> GetTopProductsRevenueAsync(int topCount = 10, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<TableRevenueData>> GetTableRevenueAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<decimal> GetTotalRevenueAsync(DateTime? fromDate = null, DateTime? toDate = null);
    }

    public class RevenueService : IRevenueService
    {
        private readonly BilliardsDbContext _context;

        public RevenueService(BilliardsDbContext context)
        {
            _context = context;
        }

        public async Task<RevenueViewModel> GetRevenueDataAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            // Nếu không có fromDate/toDate, mặc định lấy dữ liệu tháng hiện tại
            fromDate ??= startOfMonth;
            toDate ??= today.AddDays(1); // Bao gồm cả hôm nay

            var viewModel = new RevenueViewModel
            {
                FromDate = fromDate.Value,
                ToDate = toDate.Value,

                // Doanh thu tổng quan (theo khoảng thời gian được chọn) - LẤY TỪ INVOICE
                TotalRevenue = await GetTotalRevenueAsync(fromDate, toDate),
                TableRevenue = await GetTableRevenueFromInvoicesAsync(fromDate, toDate),
                ProductRevenue = await GetProductRevenueFromInvoicesAsync(fromDate, toDate),

                // Doanh thu theo thời gian - LẤY TỪ INVOICE
                TodayRevenue = await GetTotalRevenueAsync(today, today.AddDays(1)),
                WeekRevenue = await GetTotalRevenueAsync(startOfWeek, today.AddDays(1)),
                MonthRevenue = await GetTotalRevenueAsync(startOfMonth, today.AddDays(1)),
                YearRevenue = await GetTotalRevenueAsync(startOfYear, today.AddDays(1)),

                // Thống kê hóa đơn
                TotalInvoices = await GetInvoiceCountAsync(fromDate, toDate),
                TodayInvoices = await GetInvoiceCountAsync(today, today.AddDays(1)),

                // Dữ liệu biểu đồ
                DailyRevenueData = await GetDailyRevenueAsync(fromDate.Value, toDate.Value),
                MonthlyRevenueData = await GetMonthlyRevenueAsync(DateTime.Now.Year),
                TopProductsRevenue = await GetTopProductsRevenueAsync(10, fromDate, toDate),
                TableRevenueData = await GetTableRevenueAsync(fromDate, toDate),

                // Hóa đơn gần đây
                RecentInvoices = await GetRecentInvoicesAsync(20)
            };

            // Tính trung bình giá trị hóa đơn
            viewModel.AverageInvoiceValue = viewModel.TotalInvoices > 0 
                ? viewModel.TotalRevenue / viewModel.TotalInvoices 
                : 0;

            return viewModel;
        }

        public async Task<decimal> GetTotalRevenueAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Invoices.Where(i => i.TotalAmount != null && i.Status == "COMPLETED");

            if (fromDate.HasValue)
                query = query.Where(i => i.PaymentTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(i => i.PaymentTime < toDate.Value);

            return await query.SumAsync(i => i.TotalAmount ?? 0);
        }

        // Tính doanh thu bàn từ các hóa đơn (dựa trên Session.TableTotal của session tương ứng)
        private async Task<decimal> GetTableRevenueFromInvoicesAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Invoices
                .Include(i => i.Session)
                .Where(i => i.TotalAmount != null && i.Status == "COMPLETED" && 
                           i.Session != null && i.Session.TableTotal != null);

            if (fromDate.HasValue)
                query = query.Where(i => i.PaymentTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(i => i.PaymentTime < toDate.Value);

            return await query.SumAsync(i => i.Session!.TableTotal ?? 0);
        }

        // Tính doanh thu sản phẩm từ các hóa đơn (Tính trực tiếp từ OrderDetails)
        private async Task<decimal> GetProductRevenueFromInvoicesAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.OrderDetails
                .Include(od => od.Order)
                .ThenInclude(o => o.Session)
                .ThenInclude(s => s.Invoices)
                .Where(od => od.Order != null && 
                           od.Order.Session != null &&
                           od.Order.Session.Invoices.Any(inv => inv.Status == "COMPLETED") &&
                           od.UnitPrice.HasValue && 
                           od.Quantity.HasValue);

            if (fromDate.HasValue)
                query = query.Where(od => od.Order.Session.Invoices
                    .Any(inv => inv.PaymentTime >= fromDate.Value && inv.Status == "COMPLETED"));

            if (toDate.HasValue)
                query = query.Where(od => od.Order.Session.Invoices
                    .Any(inv => inv.PaymentTime < toDate.Value && inv.Status == "COMPLETED"));

            return await query.SumAsync(od => od.UnitPrice.Value * od.Quantity.Value);
        }

        private async Task<int> GetInvoiceCountAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Invoices.Where(i => i.Status == "COMPLETED");

            if (fromDate.HasValue)
                query = query.Where(i => i.PaymentTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(i => i.PaymentTime < toDate.Value);

            return await query.CountAsync();
        }

        public async Task<List<DailyRevenueData>> GetDailyRevenueAsync(DateTime fromDate, DateTime toDate)
        {
            var result = new List<DailyRevenueData>();

            for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
            {
                var nextDay = date.AddDays(1);

                var totalRevenue = await GetTotalRevenueAsync(date, nextDay);
                var tableRevenue = await GetTableRevenueFromInvoicesAsync(date, nextDay);
                var productRevenue = totalRevenue - tableRevenue;
                var invoiceCount = await GetInvoiceCountAsync(date, nextDay);

                result.Add(new DailyRevenueData
                {
                    Date = date,
                    TableRevenue = tableRevenue,
                    ProductRevenue = productRevenue,
                    TotalRevenue = totalRevenue,
                    InvoiceCount = invoiceCount
                });
            }

            return result;
        }

        public async Task<List<MonthlyRevenueData>> GetMonthlyRevenueAsync(int year)
        {
            var result = new List<MonthlyRevenueData>();

            for (int month = 1; month <= 12; month++)
            {
                var startOfMonth = new DateTime(year, month, 1);
                var endOfMonth = startOfMonth.AddMonths(1);

                var totalRevenue = await GetTotalRevenueAsync(startOfMonth, endOfMonth);
                var tableRevenue = await GetTableRevenueFromInvoicesAsync(startOfMonth, endOfMonth);
                var productRevenue = totalRevenue - tableRevenue;
                var invoiceCount = await GetInvoiceCountAsync(startOfMonth, endOfMonth);

                result.Add(new MonthlyRevenueData
                {
                    Year = year,
                    Month = month,
                    MonthName = CultureInfo.GetCultureInfo("vi-VN").DateTimeFormat.GetMonthName(month),
                    TableRevenue = tableRevenue,
                    ProductRevenue = productRevenue,
                    TotalRevenue = totalRevenue,
                    InvoiceCount = invoiceCount
                });
            }

            return result;
        }

        public async Task<List<ProductRevenueData>> GetTopProductsRevenueAsync(int topCount = 10, DateTime? fromDate = null, DateTime? toDate = null)
        {
            // Lấy doanh thu sản phẩm từ các hóa đơn đã thanh toán
            var query = _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .ThenInclude(o => o!.Session)
                .ThenInclude(s => s!.Invoices)
                .Where(od => od.Product != null && od.Order != null && 
                           od.Order.Session != null && 
                           od.Order.Session.Invoices.Any(inv => inv.Status == "COMPLETED") &&
                           od.UnitPrice != null && od.Quantity != null);

            if (fromDate.HasValue)
                query = query.Where(od => od.Order!.Session!.Invoices
                    .Any(inv => inv.PaymentTime >= fromDate.Value && inv.Status == "COMPLETED"));

            if (toDate.HasValue)
                query = query.Where(od => od.Order!.Session!.Invoices
                    .Any(inv => inv.PaymentTime < toDate.Value && inv.Status == "COMPLETED"));

            var productData = await query
                .GroupBy(od => new { od.ProductId, od.Product!.ProductName, od.Product.ProductType })
                .Select(g => new ProductRevenueData
                {
                    ProductId = g.Key.ProductId ?? 0,
                    ProductName = g.Key.ProductName ?? "",
                    ProductType = g.Key.ProductType.ToString(),
                    QuantitySold = g.Sum(od => od.Quantity ?? 0),
                    Revenue = g.Sum(od => (od.UnitPrice ?? 0) * (od.Quantity ?? 0))
                })
                .OrderByDescending(p => p.Revenue)
                .Take(topCount)
                .ToListAsync();

            // Tính phần trăm
            var totalRevenue = productData.Sum(p => p.Revenue);
            if (totalRevenue > 0)
            {
                foreach (var item in productData)
                {
                    item.Percentage = Math.Round((item.Revenue / totalRevenue) * 100, 2);
                }
            }

            return productData;
        }

        public async Task<List<TableRevenueData>> GetTableRevenueAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            // Lấy doanh thu bàn từ các hóa đơn đã thanh toán
            var query = _context.Invoices
                .Include(i => i.Session)
                .ThenInclude(s => s!.Table)
                .Where(i => i.Status == "COMPLETED" && i.Session != null && 
                           i.Session.Table != null && i.Session.TableTotal != null);

            if (fromDate.HasValue)
                query = query.Where(i => i.PaymentTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(i => i.PaymentTime < toDate.Value);

            var tableData = await query
                .GroupBy(i => new { i.Session!.TableId, i.Session.Table!.TableName, i.Session.Table.TableType })
                .Select(g => new TableRevenueData
                {
                    TableId = g.Key.TableId ?? 0,
                    TableName = g.Key.TableName ?? "",
                    TableType = g.Key.TableType ?? "",
                    Revenue = g.Sum(i => i.Session!.TableTotal ?? 0),
                    TotalSessions = g.Count(),
                    TotalHours = (double)g.Sum(i => i.Session!.TotalTime ?? 0) / 60.0 // Convert minutes to hours
                })
                .OrderByDescending(t => t.Revenue)
                .ToListAsync();

            // Tính phần trăm
            var totalRevenue = tableData.Sum(t => t.Revenue);
            if (totalRevenue > 0)
            {
                foreach (var item in tableData)
                {
                    item.Percentage = Math.Round((item.Revenue / totalRevenue) * 100, 2);
                }
            }

            return tableData;
        }

        private async Task<List<RecentInvoiceData>> GetRecentInvoicesAsync(int count = 20)
        {
            return await _context.Invoices
                .Include(i => i.Session)
                .ThenInclude(s => s!.Table)
                .Include(i => i.Cashier)
                .Where(i => i.PaymentTime != null)
                .OrderByDescending(i => i.PaymentTime)
                .Take(count)
                .Select(i => new RecentInvoiceData
                {
                    InvoiceId = i.InvoiceId,
                    PaymentTime = i.PaymentTime ?? DateTime.MinValue,
                    TableName = i.Session != null && i.Session.Table != null ? i.Session.Table.TableName : "N/A",
                    TotalAmount = i.TotalAmount ?? 0,
                    PaymentMethod = i.PaymentMethod ?? "N/A",
                    CashierName = i.Cashier != null ? i.Cashier.FullName : "N/A"
                })
                .ToListAsync();
        }
    }
} 