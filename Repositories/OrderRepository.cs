using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly BilliardsDbContext _context;

        public OrderRepository(BilliardsDbContext context)
        {
            _context = context;
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders.FindAsync(id);
        }

        public async Task<Order?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.Session)
                .ThenInclude(s => s.Table)
                .Include(o => o.OrderNavigation) // Employee
                .FirstOrDefaultAsync(o => o.OrderId == id);
        }

        public async Task<List<Order>> GetAllAsync()
        {
            return await _context.Orders
                .Include(o => o.Session)
                .ThenInclude(s => s.Table)
                .Include(o => o.OrderNavigation)
                .ToListAsync();
        }

        public async Task<List<Order>> GetBySessionIdAsync(int sessionId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.SessionId == sessionId)
                .ToListAsync();
        }

        public async Task<List<Order>> GetByStatusAsync(string status)
        {
            return await _context.Orders
                .Include(o => o.Session)
                .ThenInclude(s => s.Table)
                .Include(o => o.OrderNavigation)
                .Where(o => o.Status == status)
                .ToListAsync();
        }

        public async Task<List<Order>> GetPendingOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.Session)
                .ThenInclude(s => s.Table)
                .Include(o => o.OrderNavigation)
                .Where(o => o.Status == "PENDING")
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetPendingCountAsync()
        {
            return await _context.Orders
                .CountAsync(o => o.Status == "PENDING");
        }

        public async Task<Order> CreateAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order> UpdateAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task DeleteAsync(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
} 