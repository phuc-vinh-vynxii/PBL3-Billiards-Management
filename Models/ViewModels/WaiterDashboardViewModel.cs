using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Models.ViewModels
{
    public class WaiterDashboardViewModel
    {
        public IEnumerable<Table>? Tables { get; set; }
        public IEnumerable<Session>? ActiveSessions { get; set; }
        public IEnumerable<Order>? RecentOrders { get; set; }
        public int AvailableTableCount { get; set; }
        public int OccupiedTableCount { get; set; }
        public int BookedTableCount { get; set; }
        public decimal TodayRevenue { get; set; }
    }
}
