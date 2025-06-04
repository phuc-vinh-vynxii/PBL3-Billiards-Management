using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Models.ViewModels
{
    public class ServingDashboardViewModel
    {
        public List<Table> Tables { get; set; } = new List<Table>();
        public List<string> UserPermissions { get; set; } = new List<string>();
        
        // Statistics
        public int TotalActiveTables { get; set; }
        public int TotalAvailableTables { get; set; }
        public int TodaySessionsCount { get; set; }
        public decimal TodayServiceRevenue { get; set; }
        
        // Permission helpers - Serving staff default has booking permission
        public bool CanCreateBookings => true; // Default permission for serving staff
        public bool CanStartSessions => HasPermission("SESSION_START");
        public bool CanEndSessions => HasPermission("SESSION_END"); 
        public bool CanManageOrders => HasPermission("ORDER_MANAGE");
        public bool CanProcessPayments => HasPermission("PAYMENT_PROCESS");
        public bool CanViewProducts => HasPermission("PRODUCT_VIEW");
        public bool CanAccessBooking => true; // Default permission for serving staff
        public bool CanAccessTableService => HasPermission("TABLE_SERVICE");
        
        private bool HasPermission(string permission)
        {
            return UserPermissions.Contains(permission);
        }
    }
} 