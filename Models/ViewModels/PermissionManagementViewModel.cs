using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Models.ViewModels
{
    public class PermissionManagementViewModel
    {
        public IEnumerable<Employee> Employees { get; set; } = new List<Employee>();
        public IEnumerable<Permission> Permissions { get; set; } = new List<Permission>();
        public IEnumerable<EmployeePermissionDto> EmployeePermissions { get; set; } = new List<EmployeePermissionDto>();
        public Dictionary<string, List<Permission>> PermissionsByCategory { get; set; } = new Dictionary<string, List<Permission>>();
    }

    public class EmployeePermissionDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public string PermissionDisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime GrantedAt { get; set; }
        public string? GrantedByName { get; set; }
        public bool IsActive { get; set; }
    }

    public class GrantPermissionRequest
    {
        public int EmployeeId { get; set; }
        public List<int> PermissionIds { get; set; } = new List<int>();
    }
} 