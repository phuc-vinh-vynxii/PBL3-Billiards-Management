using System.ComponentModel.DataAnnotations;

namespace BilliardsManagement.Models.Entities;

public class ShiftAssignment
{
    public int ShiftAssignmentId { get; set; }
    
    [Required]
    public int EmployeeId { get; set; }
    
    [Required]
    public int ShiftId { get; set; }
    
    [Required]
    public DateOnly AssignedDate { get; set; }
    
    [Required]
    public DayOfWeek DayOfWeek { get; set; }
    
    [MaxLength(200)]
    public string? Notes { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public int? CreatedBy { get; set; }
    
    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
    public virtual Shift Shift { get; set; } = null!;
    public virtual Employee? CreatedByEmployee { get; set; }
} 