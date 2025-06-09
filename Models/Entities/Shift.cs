using System.ComponentModel.DataAnnotations;

namespace BilliardsManagement.Models.Entities;

public class Shift
{
    public int ShiftId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string ShiftName { get; set; } = string.Empty;
    
    [Required]
    public TimeOnly StartTime { get; set; }
    
    [Required]
    public TimeOnly EndTime { get; set; }
    
    [MaxLength(200)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation properties
    public virtual ICollection<ShiftAssignment> ShiftAssignments { get; set; } = new List<ShiftAssignment>();
} 