using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Services
{
    public interface IOrderManagementService
    {
        Task<(bool Success, string Message)> AddOrderAsync(int tableId, int productId, int quantity, int employeeId);
        Task<(bool Success, string Message)> RemoveOrderDetailAsync(int orderDetailId);
        Task<(bool Success, string Message)> UpdateOrderDetailQuantityAsync(int orderDetailId, int changeAmount);
        Task<OrderDetail?> GetOrderDetailAsync(int orderDetailId);
        Task<List<OrderDetail>> GetOrderDetailsBySessionAsync(int sessionId);
        Task<Order?> GetOrCreateOrderForSessionAsync(int sessionId, int employeeId);
    }
} 