using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Services
{
    public interface IOrderService
    {
        Task<List<dynamic>> GetCurrentOrderAsync(int sessionId);
        Task<(bool Success, string Message, int? RemainingQuantity)> AddOrderAsync(int tableId, int productId, int quantity, int employeeId);
        Task<bool> RemoveOrderDetailAsync(int orderDetailId);
        Task<(bool Success, string Message, bool Removed, int NewQuantity, decimal Subtotal)> UpdateOrderDetailQuantityAsync(int orderDetailId, int changeAmount);
        Task<List<dynamic>> GetOrderDetailInfoAsync();
        Task<bool> CleanupOrphanedOrdersAsync();
    }
} 