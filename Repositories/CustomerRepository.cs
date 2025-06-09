using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly BilliardsDbContext _context;

        public CustomerRepository(BilliardsDbContext context)
        {
            _context = context;
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _context.Customers.FindAsync(id);
        }

        public async Task<Customer?> GetByPhoneAsync(string phone)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == phone);
        }

        public async Task<List<Customer>> GetAllAsync()
        {
            return await _context.Customers
                .OrderBy(c => c.FullName)
                .ToListAsync();
        }

        public async Task<List<Customer>> SearchAsync(string query)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 2)
            {
                return new List<Customer>();
            }

            return await _context.Customers
                .Where(c => (c.Phone != null && c.Phone.Contains(query)) || 
                           (c.FullName != null && c.FullName.Contains(query)))
                .Take(5)
                .ToListAsync();
        }

        public async Task<bool> PhoneExistsAsync(string phone)
        {
            return await _context.Customers
                .AnyAsync(c => c.Phone == phone);
        }

        public async Task<Customer> CreateAsync(Customer customer)
        {
            customer.LoyaltyPoints = 0; // Initialize loyalty points
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task<Customer> UpdateAsync(Customer customer)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task DeleteAsync(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
} 