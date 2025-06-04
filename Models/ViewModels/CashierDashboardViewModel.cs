using System.ComponentModel.DataAnnotations;

namespace BilliardsManagement.Models.ViewModels
{
    public class CashierDashboardViewModel
    {
        // Dashboard Statistics
        public int TotalActiveTables { get; set; }
        public int TotalAvailableTables { get; set; }
        public decimal TodayRevenue { get; set; }
        public int TodayTransactions { get; set; }
        public int PendingOrders { get; set; }
        
        // Permission-based features
        public List<string> UserPermissions { get; set; } = new List<string>();
        
        // Quick access data
        public List<TableQuickInfo> TableStatus { get; set; } = new List<TableQuickInfo>();
        public List<RecentTransaction> RecentTransactions { get; set; } = new List<RecentTransaction>();
        public List<PendingOrderInfo> PendingOrdersList { get; set; } = new List<PendingOrderInfo>();
        
        // Helper methods
        public bool HasPermission(string permission) => UserPermissions.Contains(permission);
        public bool CanManageTables => HasPermission("TABLE_VIEW") || HasPermission("TABLE_MANAGE");
        public bool CanViewBookings => HasPermission("BOOKING_VIEW");
        public bool CanViewProducts => HasPermission("PRODUCT_VIEW");
        public bool CanViewRevenue => HasPermission("REVENUE_VIEW");
        public bool CanManageOrders => HasPermission("ORDER_VIEW") || HasPermission("ORDER_MANAGE");
        public bool CanProcessPayments => HasPermission("PAYMENT_PROCESS");
        public bool CanTransferTables => HasPermission("TABLE_TRANSFER");
    }
    
    public class TableQuickInfo
    {
        public int TableId { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public decimal CurrentBill { get; set; }
    }
    
    public class RecentTransaction
    {
        public int InvoiceId { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
    
    public class PendingOrderInfo
    {
        public int OrderId { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime OrderTime { get; set; }
        public string Status { get; set; } = string.Empty;
    }
} 