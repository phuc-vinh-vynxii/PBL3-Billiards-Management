using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Repositories
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(int id);
        Task<List<Product>> GetAllAsync();
        Task<List<Product>> GetByTypeAsync(ProductType productType);
        Task<List<Product>> GetFoodAndBeverageAsync();
        Task<List<Product>> GetEquipmentAndSuppliesAsync();
        Task<List<Product>> GetAvailableAsync();
        Task<List<Product>> GetAvailableFoodAndBeverageAsync();
        Task<List<Product>> GetByStatusAsync(string status);
        Task<int> GetCountAsync();
        Task<int> GetCountByTypeAsync(ProductType productType);
        Task<int> GetLowStockCountAsync(int threshold = 10);
        Task<int> GetLowStockCountByTypeAsync(ProductType productType, int threshold = 10);
        Task<decimal> GetTotalValueAsync();
        Task<decimal> GetTotalValueByTypeAsync(ProductType productType);
        Task<Product> CreateAsync(Product product);
        Task<Product> UpdateAsync(Product product);
        Task DeleteAsync(int id);
        Task SaveChangesAsync();
    }
} 