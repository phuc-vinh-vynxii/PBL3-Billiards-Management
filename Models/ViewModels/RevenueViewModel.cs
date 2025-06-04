using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Models.ViewModels
{
    public class RevenueViewModel
    {
        // Doanh thu tổng quan
        public decimal TotalRevenue { get; set; }
        public decimal TableRevenue { get; set; }
        public decimal ProductRevenue { get; set; }
        
        // Doanh thu theo thời gian
        public decimal TodayRevenue { get; set; }
        public decimal WeekRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal YearRevenue { get; set; }
        
        // Thống kê chi tiết
        public int TotalInvoices { get; set; }
        public int TodayInvoices { get; set; }
        public decimal AverageInvoiceValue { get; set; }
        
        // Dữ liệu biểu đồ
        public List<DailyRevenueData> DailyRevenueData { get; set; } = new List<DailyRevenueData>();
        public List<MonthlyRevenueData> MonthlyRevenueData { get; set; } = new List<MonthlyRevenueData>();
        public List<ProductRevenueData> TopProductsRevenue { get; set; } = new List<ProductRevenueData>();
        public List<TableRevenueData> TableRevenueData { get; set; } = new List<TableRevenueData>();
        
        // Dữ liệu để filter
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        
        // Danh sách hóa đơn gần đây
        public List<RecentInvoiceData> RecentInvoices { get; set; } = new List<RecentInvoiceData>();
    }
    
    public class DailyRevenueData
    {
        public DateTime Date { get; set; }
        public decimal TableRevenue { get; set; }
        public decimal ProductRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public int InvoiceCount { get; set; }
    }
    
    public class MonthlyRevenueData
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal TableRevenue { get; set; }
        public decimal ProductRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public int InvoiceCount { get; set; }
    }
    
    public class ProductRevenueData
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Percentage { get; set; }
    }
    
    public class TableRevenueData
    {
        public int TableId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string TableType { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int TotalSessions { get; set; }
        public double TotalHours { get; set; }
        public decimal Percentage { get; set; }
    }
    
    public class RecentInvoiceData
    {
        public int InvoiceId { get; set; }
        public DateTime PaymentTime { get; set; }
        public string TableName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
    }
} 