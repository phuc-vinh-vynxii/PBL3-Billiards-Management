using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;

namespace BilliardsManagement.Services;

public class ShiftAssignResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public interface IShiftService
{
    Task<List<Shift>> GetAllShiftsAsync();
    Task<List<Employee>> GetAllEmployeesAsync();
    Task<ShiftManagementViewModel> GetWeeklyScheduleAsync(DateTime weekStart);
    Task<bool> CreateShiftAsync(CreateShiftViewModel model);
    Task<bool> UpdateShiftAsync(int shiftId, CreateShiftViewModel model);
    Task<bool> DeleteShiftAsync(int shiftId);
    Task<ShiftAssignResult> AssignShiftAsync(AssignShiftRequest request, int managerId);
    Task<bool> UnassignShiftAsync(int assignmentId);
    Task<List<ShiftAssignment>> GetEmployeeShiftsAsync(int employeeId, DateTime weekStart);
    Task<bool> BulkAssignShiftsAsync(List<AssignShiftRequest> requests, int managerId);
} 