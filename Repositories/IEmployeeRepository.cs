using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Repositories
{
    public interface IEmployeeRepository
    {
        Task<Employee?> GetByIdAsync(int id);
        Task<Employee?> GetByUsernameAsync(string username);
        Task<Employee?> GetByUsernameAndPasswordAsync(string username, string password);
        Task<List<Employee>> GetAllAsync();
        Task<List<Employee>> GetByPositionAsync(string position);
        Task<List<Employee>> GetNonManagersAsync();
        Task<int> GetCountAsync();
        Task<bool> IsEmailExistsAsync(string email, int? excludeEmployeeId = null);
        Task<bool> IsUsernameExistsAsync(string username, int? excludeEmployeeId = null);
        Task<Employee> CreateAsync(Employee employee);
        Task<Employee> UpdateAsync(Employee employee);
        Task DeleteAsync(int id);
        Task SaveChangesAsync();
    }
} 