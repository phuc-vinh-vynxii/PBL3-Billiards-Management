using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;

namespace BilliardsManagement.Services
{
    public interface IProductService
    {
        Task<InventoryViewModel> GetFoodAndBeverageInventoryAsync();
        Task<InventoryViewModel> GetEquipmentInventoryAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<bool> SaveProductAsync(Product product);
        Task<bool> DeleteProductAsync(int id);
        Task<List<dynamic>> GetAvailableProductsAsync();
    }
} 