using System.ComponentModel.DataAnnotations;

namespace BilliardsManagement.Models.Entities
{
    public class Permission
    {
        [Key]
        public int PermissionId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string PermissionName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        // Navigation property
        public virtual ICollection<EmployeePermission> EmployeePermissions { get; set; } = new List<EmployeePermission>();
    }
} 