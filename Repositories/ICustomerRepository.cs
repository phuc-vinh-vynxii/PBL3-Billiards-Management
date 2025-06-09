using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Repositories
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(int id);
        Task<Customer?> GetByPhoneAsync(string phone);
        Task<List<Customer>> GetAllAsync();
        Task<List<Customer>> SearchAsync(string query);
        Task<bool> PhoneExistsAsync(string phone);
        Task<Customer> CreateAsync(Customer customer);
        Task<Customer> UpdateAsync(Customer customer);
        Task DeleteAsync(int id);
        Task SaveChangesAsync();
    }
} 