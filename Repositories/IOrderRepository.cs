using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Repositories
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(int id);
        Task<Order?> GetByIdWithDetailsAsync(int id);
        Task<List<Order>> GetAllAsync();
        Task<List<Order>> GetBySessionIdAsync(int sessionId);
        Task<List<Order>> GetByStatusAsync(string status);
        Task<List<Order>> GetPendingOrdersAsync();
        Task<int> GetPendingCountAsync();
        Task<Order> CreateAsync(Order order);
        Task<Order> UpdateAsync(Order order);
        Task DeleteAsync(int id);
        Task SaveChangesAsync();
    }
} 