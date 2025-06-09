using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Models.ViewModels;

public class ShiftManagementViewModel
{
    public List<Shift> Shifts { get; set; } = new List<Shift>();
    public List<Employee> Employees { get; set; } = new List<Employee>();
    public List<ShiftAssignmentDisplayModel> WeeklyAssignments { get; set; } = new List<ShiftAssignmentDisplayModel>();
    public DateTime CurrentWeekStart { get; set; }
    public DateTime CurrentWeekEnd { get; set; }
}

public class ShiftAssignmentDisplayModel
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public Dictionary<DayOfWeek, List<ShiftInfo>> DailyShifts { get; set; } = new Dictionary<DayOfWeek, List<ShiftInfo>>();
}

public class ShiftInfo
{
    public int ShiftId { get; set; }
    public int? AssignmentId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsAssigned { get; set; }
    public string? Notes { get; set; }
}

public class CreateShiftViewModel
{
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Description { get; set; }
}

public class AssignShiftRequest
{
    public int EmployeeId { get; set; }
    public int ShiftId { get; set; }
    public DateOnly AssignedDate { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string? Notes { get; set; }
} 