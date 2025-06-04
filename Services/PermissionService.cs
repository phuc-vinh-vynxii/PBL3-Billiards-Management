using BilliardsManagement.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BilliardsManagement.Services
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(int employeeId, string permissionName);
        Task<List<string>> GetEmployeePermissionsAsync(int employeeId);
        Task<bool> HasAnyPermissionAsync(int employeeId, params string[] permissionNames);
        Task<Dictionary<string, List<Permission>>> GetEmployeePermissionsByCategoryAsync(int employeeId);
        Task<List<Permission>> GetEmployeePermissionListAsync(int employeeId);
    }

    public class PermissionService : IPermissionService
    {
        private readonly BilliardsDbContext _context;

        public PermissionService(BilliardsDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasPermissionAsync(int employeeId, string permissionName)
        {
            var employee = await _context.Employees
                .Include(e => e.EmployeePermissions)
                .ThenInclude(ep => ep.Permission)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null) return false;

            // Managers have all permissions
            if (employee.Position?.ToUpper() == "MANAGER") return true;

            return employee.EmployeePermissions
                .Any(ep => ep.IsActive && 
                          ep.Permission.IsActive && 
                          ep.Permission.PermissionName == permissionName);
        }

        public async Task<List<string>> GetEmployeePermissionsAsync(int employeeId)
        {
            var employee = await _context.Employees
                .Include(e => e.EmployeePermissions)
                .ThenInclude(ep => ep.Permission)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null) return new List<string>();

            // Managers have all permissions
            if (employee.Position?.ToUpper() == "MANAGER")
            {
                return await _context.Permissions
                    .Where(p => p.IsActive)
                    .Select(p => p.PermissionName)
                    .ToListAsync();
            }

            return employee.EmployeePermissions
                .Where(ep => ep.IsActive && ep.Permission.IsActive)
                .Select(ep => ep.Permission.PermissionName)
                .ToList();
        }

        public async Task<bool> HasAnyPermissionAsync(int employeeId, params string[] permissionNames)
        {
            var employee = await _context.Employees
                .Include(e => e.EmployeePermissions)
                .ThenInclude(ep => ep.Permission)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null) return false;

            // Managers have all permissions
            if (employee.Position?.ToUpper() == "MANAGER") return true;

            return employee.EmployeePermissions
                .Any(ep => ep.IsActive && 
                          ep.Permission.IsActive && 
                          permissionNames.Contains(ep.Permission.PermissionName));
        }

        public async Task<Dictionary<string, List<Permission>>> GetEmployeePermissionsByCategoryAsync(int employeeId)
        {
            var employee = await _context.Employees
                .Include(e => e.EmployeePermissions)
                .ThenInclude(ep => ep.Permission)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null) return new Dictionary<string, List<Permission>>();

            List<Permission> permissions;

            // Managers have all permissions
            if (employee.Position?.ToUpper() == "MANAGER")
            {
                permissions = await _context.Permissions
                    .Where(p => p.IsActive)
                    .ToListAsync();
            }
            else
            {
                permissions = employee.EmployeePermissions
                    .Where(ep => ep.IsActive && ep.Permission.IsActive)
                    .Select(ep => ep.Permission)
                    .ToList();
            }

            return permissions
                .GroupBy(p => p.Category)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public async Task<List<Permission>> GetEmployeePermissionListAsync(int employeeId)
        {
            var employee = await _context.Employees
                .Include(e => e.EmployeePermissions)
                .ThenInclude(ep => ep.Permission)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null) return new List<Permission>();

            // Managers have all permissions
            if (employee.Position?.ToUpper() == "MANAGER")
            {
                return await _context.Permissions
                    .Where(p => p.IsActive)
                    .ToListAsync();
            }

            return employee.EmployeePermissions
                .Where(ep => ep.IsActive && ep.Permission.IsActive)
                .Select(ep => ep.Permission)
                .ToList();
        }
    }
} 