using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;

namespace BilliardsManagement.Services
{
    public interface IStaffService
    {
        Task<StaffManagementViewModel> GetStaffManagementDataAsync();
        Task<Employee?> GetEmployeeByIdAsync(int id);
        Task<(bool Success, string Message)> CreateEmployeeAsync(CreateEmployeeViewModel model);
        Task<(bool Success, string Message)> UpdateEmployeeAsync(EditEmployeeViewModel model);
        Task<(bool Success, string Message)> DeleteEmployeeAsync(int id);
        Task<(bool Success, string Message)> ResetPasswordAsync(int id, string newPassword);
    }
} 