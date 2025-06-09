using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly BilliardsDbContext _context;

        public EmployeeRepository(BilliardsDbContext context)
        {
            _context = context;
        }

        public async Task<Employee?> GetByIdAsync(int id)
        {
            return await _context.Employees.FindAsync(id);
        }

        public async Task<Employee?> GetByUsernameAsync(string username)
        {
            return await _context.Employees
                .FirstOrDefaultAsync(e => e.Username == username);
        }

        public async Task<Employee?> GetByUsernameAndPasswordAsync(string username, string password)
        {
            return await _context.Employees
                .FirstOrDefaultAsync(e => e.Username == username && e.Password == password);
        }

        public async Task<List<Employee>> GetAllAsync()
        {
            return await _context.Employees.ToListAsync();
        }

        public async Task<List<Employee>> GetByPositionAsync(string position)
        {
            return await _context.Employees
                .Where(e => e.Position == position)
                .ToListAsync();
        }

        public async Task<List<Employee>> GetNonManagersAsync()
        {
            return await _context.Employees
                .Where(e => e.Position != "MANAGER")
                .OrderBy(e => e.FullName)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Employees.CountAsync();
        }

        public async Task<bool> IsEmailExistsAsync(string email, int? excludeEmployeeId = null)
        {
            var query = _context.Employees.Where(e => e.Email == email);
            
            if (excludeEmployeeId.HasValue)
                query = query.Where(e => e.EmployeeId != excludeEmployeeId.Value);
                
            return await query.AnyAsync();
        }

        public async Task<bool> IsUsernameExistsAsync(string username, int? excludeEmployeeId = null)
        {
            var query = _context.Employees.Where(e => e.Username == username);
            
            if (excludeEmployeeId.HasValue)
                query = query.Where(e => e.EmployeeId != excludeEmployeeId.Value);
                
            return await query.AnyAsync();
        }

        public async Task<Employee> CreateAsync(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            return employee;
        }

        public async Task<Employee> UpdateAsync(Employee employee)
        {
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
            return employee;
        }

        public async Task DeleteAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
} 