using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Models.ViewModels
{
    public class StaffManagementViewModel
    {
        public IEnumerable<Employee> Employees { get; set; } = new List<Employee>();
        public StaffStatistics Statistics { get; set; } = new StaffStatistics();
    }

    public class StaffStatistics
    {
        public int TotalEmployees { get; set; }
        public int ManagerCount { get; set; }
        public int CashierCount { get; set; }
        public int ServingCount { get; set; }
        public int NewEmployeesThisMonth { get; set; }
    }

    public class CreateEmployeeViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class EditEmployeeViewModel
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }
} 