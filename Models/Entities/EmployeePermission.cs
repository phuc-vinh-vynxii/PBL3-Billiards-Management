using System.ComponentModel.DataAnnotations;

namespace BilliardsManagement.Models.Entities
{
    public class EmployeePermission
    {
        [Key]
        public int EmployeePermissionId { get; set; }
        
        [Required]
        public int EmployeeId { get; set; }
        
        [Required]
        public int PermissionId { get; set; }
        
        public DateTime GrantedAt { get; set; } = DateTime.Now;
        
        public int? GrantedBy { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual Employee Employee { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
        public virtual Employee? GrantedByEmployee { get; set; }
    }
} 