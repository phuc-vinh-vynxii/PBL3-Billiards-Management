using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Services
{
    public interface ICustomerService
    {
        Task<(bool Success, string Message, Customer? Customer)> CreateCustomerAsync(Customer customer);
        Task<List<dynamic>> SearchCustomersAsync(string query);
    }
} 