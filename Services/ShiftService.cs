using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BilliardsManagement.Services;

public class ShiftService : IShiftService
{
    private readonly BilliardsDbContext _context;

    public ShiftService(BilliardsDbContext context)
    {
        _context = context;
    }

    public async Task<List<Shift>> GetAllShiftsAsync()
    {
        return await _context.Shifts
            .Where(s => s.IsActive)
            .OrderBy(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<List<Employee>> GetAllEmployeesAsync()
    {
        return await _context.Employees
            .Where(e => e.Position != null && e.Position.ToUpper() != "MANAGER")
            .OrderBy(e => e.FullName)
            .ToListAsync();
    }

    public async Task<ShiftManagementViewModel> GetWeeklyScheduleAsync(DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(6);
        
        var shifts = await GetAllShiftsAsync();
        var employees = await GetAllEmployeesAsync();
        
        var assignments = await _context.ShiftAssignments
            .Include(sa => sa.Employee)
            .Include(sa => sa.Shift)
            .Where(sa => sa.AssignedDate >= DateOnly.FromDateTime(weekStart) && 
                        sa.AssignedDate <= DateOnly.FromDateTime(weekEnd) &&
                        sa.IsActive)
            .ToListAsync();

        var weeklyAssignments = new List<ShiftAssignmentDisplayModel>();

        foreach (var employee in employees)
        {
            var employeeModel = new ShiftAssignmentDisplayModel
            {
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.FullName ?? "N/A",
                Position = employee.Position ?? "N/A",
                DailyShifts = new Dictionary<DayOfWeek, List<ShiftInfo>>()
            };

            // Initialize all days
            for (int i = 0; i < 7; i++)
            {
                var dayOfWeek = (DayOfWeek)i;
                employeeModel.DailyShifts[dayOfWeek] = new List<ShiftInfo>();
                
                foreach (var shift in shifts)
                {
                    var assignment = assignments.FirstOrDefault(a => 
                        a.EmployeeId == employee.EmployeeId && 
                        a.ShiftId == shift.ShiftId && 
                        a.DayOfWeek == dayOfWeek);

                    employeeModel.DailyShifts[dayOfWeek].Add(new ShiftInfo
                    {
                        ShiftId = shift.ShiftId,
                        AssignmentId = assignment?.ShiftAssignmentId,
                        ShiftName = shift.ShiftName,
                        StartTime = shift.StartTime,
                        EndTime = shift.EndTime,
                        IsAssigned = assignment != null,
                        Notes = assignment?.Notes
                    });
                }
            }

            weeklyAssignments.Add(employeeModel);
        }

        return new ShiftManagementViewModel
        {
            Shifts = shifts,
            Employees = employees,
            WeeklyAssignments = weeklyAssignments,
            CurrentWeekStart = weekStart,
            CurrentWeekEnd = weekEnd
        };
    }

    public async Task<bool> CreateShiftAsync(CreateShiftViewModel model)
    {
        try
        {
            var shift = new Shift
            {
                ShiftName = model.ShiftName,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Description = model.Description,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Shifts.Add(shift);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateShiftAsync(int shiftId, CreateShiftViewModel model)
    {
        try
        {
            var shift = await _context.Shifts.FindAsync(shiftId);
            if (shift == null) return false;

            shift.ShiftName = model.ShiftName;
            shift.StartTime = model.StartTime;
            shift.EndTime = model.EndTime;
            shift.Description = model.Description;

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteShiftAsync(int shiftId)
    {
        try
        {
            var shift = await _context.Shifts.FindAsync(shiftId);
            if (shift == null) return false;

            // Soft delete
            shift.IsActive = false;
            
            // Also deactivate all assignments for this shift
            var assignments = await _context.ShiftAssignments
                .Where(sa => sa.ShiftId == shiftId && sa.IsActive)
                .ToListAsync();
            
            foreach (var assignment in assignments)
            {
                assignment.IsActive = false;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ShiftAssignResult> AssignShiftAsync(AssignShiftRequest request, int managerId)
    {
        try
        {
            // Validate input
            if (request == null)
                return new ShiftAssignResult { Success = false, Message = "Request object is null" };

            Console.WriteLine($"AssignShiftAsync called with: EmployeeId={request.EmployeeId}, ShiftId={request.ShiftId}, Date={request.AssignedDate}, DayOfWeek={request.DayOfWeek}");

            // Validate required fields
            if (request.EmployeeId <= 0)
                return new ShiftAssignResult { Success = false, Message = "EmployeeId không hợp lệ" };
                
            if (request.ShiftId <= 0)
                return new ShiftAssignResult { Success = false, Message = "ShiftId không hợp lệ" };
                
            if (request.AssignedDate == default(DateOnly))
                return new ShiftAssignResult { Success = false, Message = "Ngày phân công không hợp lệ" };

            // Check if employee exists and is not a manager
            var employee = await _context.Employees.FindAsync(request.EmployeeId);
            if (employee == null)
                return new ShiftAssignResult { Success = false, Message = "Nhân viên không tồn tại" };
            
            if (employee.Position?.ToUpper() == "MANAGER")
                return new ShiftAssignResult { Success = false, Message = "Không thể phân công ca cho quản lý" };

            // Check if shift exists and is active
            var shift = await _context.Shifts.FindAsync(request.ShiftId);
            if (shift == null)
                return new ShiftAssignResult { Success = false, Message = "Ca làm việc không tồn tại" };
            
            if (!shift.IsActive)
                return new ShiftAssignResult { Success = false, Message = "Ca làm việc đã bị vô hiệu hóa" };

            // Check if assignment already exists
            var existingAssignment = await _context.ShiftAssignments
                .FirstOrDefaultAsync(sa => sa.EmployeeId == request.EmployeeId &&
                                         sa.ShiftId == request.ShiftId &&
                                         sa.AssignedDate == request.AssignedDate &&
                                         sa.IsActive);

            if (existingAssignment != null)
                return new ShiftAssignResult { Success = false, Message = $"Nhân viên {employee.FullName} đã được phân công ca {shift.ShiftName} trong ngày này" };

            // Check for conflicting shifts (same employee, same date, overlapping time)
            var conflictingShifts = await _context.ShiftAssignments
                .Include(sa => sa.Shift)
                .Where(sa => sa.EmployeeId == request.EmployeeId &&
                           sa.AssignedDate == request.AssignedDate &&
                           sa.IsActive &&
                           sa.ShiftId != request.ShiftId)
                .ToListAsync();

            foreach (var conflictShift in conflictingShifts)
            {
                // Check for time overlap
                if (DoTimesOverlap(shift.StartTime, shift.EndTime, 
                                 conflictShift.Shift.StartTime, conflictShift.Shift.EndTime))
                {
                    return new ShiftAssignResult { 
                        Success = false, 
                        Message = $"Ca {shift.ShiftName} bị trùng thời gian với ca {conflictShift.Shift.ShiftName} đã được phân công cho nhân viên {employee.FullName}" 
                    };
                }
            }

            var assignment = new ShiftAssignment
            {
                EmployeeId = request.EmployeeId,
                ShiftId = request.ShiftId,
                AssignedDate = request.AssignedDate,
                DayOfWeek = request.DayOfWeek,
                Notes = request.Notes,
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedBy = managerId
            };

            _context.ShiftAssignments.Add(assignment);
            await _context.SaveChangesAsync();
            
            return new ShiftAssignResult { 
                Success = true, 
                Message = $"Phân công ca {shift.ShiftName} cho nhân viên {employee.FullName} thành công" 
            };
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            Console.WriteLine($"Error in AssignShiftAsync: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return new ShiftAssignResult { 
                Success = false, 
                Message = $"Có lỗi xảy ra khi phân công ca: {ex.Message}" 
            };
        }
    }

    private bool DoTimesOverlap(TimeOnly start1, TimeOnly end1, TimeOnly start2, TimeOnly end2)
    {
        // Handle cases where shift crosses midnight
        if (end1 < start1) // First shift crosses midnight
        {
            if (end2 < start2) // Second shift also crosses midnight
            {
                return true; // Both cross midnight, they overlap
            }
            else
            {
                // First crosses midnight, second doesn't
                return start2 < end1 || start1 < end2;
            }
        }
        else if (end2 < start2) // Second shift crosses midnight, first doesn't
        {
            return start1 < end2 || start2 < end1;
        }
        else // Neither shift crosses midnight
        {
            return start1 < end2 && start2 < end1;
        }
    }

    public async Task<bool> UnassignShiftAsync(int assignmentId)
    {
        try
        {
            var assignment = await _context.ShiftAssignments.FindAsync(assignmentId);
            if (assignment == null) return false;

            assignment.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<ShiftAssignment>> GetEmployeeShiftsAsync(int employeeId, DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(6);
        
        return await _context.ShiftAssignments
            .Include(sa => sa.Shift)
            .Where(sa => sa.EmployeeId == employeeId &&
                        sa.AssignedDate >= DateOnly.FromDateTime(weekStart) &&
                        sa.AssignedDate <= DateOnly.FromDateTime(weekEnd) &&
                        sa.IsActive)
            .OrderBy(sa => sa.AssignedDate)
            .ThenBy(sa => sa.Shift.StartTime)
            .ToListAsync();
    }

    public async Task<bool> BulkAssignShiftsAsync(List<AssignShiftRequest> requests, int managerId)
    {
        try
        {
            var assignments = new List<ShiftAssignment>();

            foreach (var request in requests)
            {
                // Check if assignment already exists
                var existingAssignment = await _context.ShiftAssignments
                    .FirstOrDefaultAsync(sa => sa.EmployeeId == request.EmployeeId &&
                                             sa.ShiftId == request.ShiftId &&
                                             sa.AssignedDate == request.AssignedDate &&
                                             sa.IsActive);

                if (existingAssignment == null)
                {
                    assignments.Add(new ShiftAssignment
                    {
                        EmployeeId = request.EmployeeId,
                        ShiftId = request.ShiftId,
                        AssignedDate = request.AssignedDate,
                        DayOfWeek = request.DayOfWeek,
                        Notes = request.Notes,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        CreatedBy = managerId
                    });
                }
            }

            if (assignments.Any())
            {
                _context.ShiftAssignments.AddRange(assignments);
                await _context.SaveChangesAsync();
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
} 