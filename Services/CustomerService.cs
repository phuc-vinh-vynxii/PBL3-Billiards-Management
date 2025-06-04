using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly BilliardsDbContext _context;

        public CustomerService(BilliardsDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, Customer? Customer)> CreateCustomerAsync(Customer customer)
        {
            if (string.IsNullOrEmpty(customer.Phone) || string.IsNullOrEmpty(customer.FullName))
            {
                return (false, "Số điện thoại và tên khách hàng là bắt buộc", null);
            }

            // Check if phone number already exists
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == customer.Phone);
            if (existingCustomer != null)
            {
                return (false, "Số điện thoại đã tồn tại", null);
            }

            customer.LoyaltyPoints = 0; // Initialize loyalty points
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return (true, "Tạo khách hàng thành công", customer);
        }

        public async Task<List<dynamic>> SearchCustomersAsync(string query)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 2)
            {
                return new List<dynamic>();
            }

            var customers = await _context.Customers
                .Where(c => (c.Phone != null && c.Phone.Contains(query)) || 
                           (c.FullName != null && c.FullName.Contains(query)))
                .Take(5)
                .Select(c => new { c.CustomerId, c.FullName, c.Phone })
                .ToListAsync();

            return customers.Cast<dynamic>().ToList();
        }
    }
} 