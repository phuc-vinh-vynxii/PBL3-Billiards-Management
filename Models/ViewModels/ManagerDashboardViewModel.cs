using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Models.ViewModels;

public class ManagerDashboardViewModel
{
    public int TotalTables { get; set; }
    public int AvailableTables { get; set; }
    public int BrokenTables { get; set; }
    public int MaintenanceTables { get; set; }
    public decimal TodayRevenue { get; set; }
    public int TotalEmployees { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public IEnumerable<Table>? Tables { get; set; }
    public IEnumerable<Employee>? Employees { get; set; }
}