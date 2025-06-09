using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly BilliardsDbContext _context;

        public ProductRepository(BilliardsDbContext context)
        {
            _context = context;
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<List<Product>> GetAllAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<List<Product>> GetByTypeAsync(ProductType productType)
        {
            return await _context.Products
                .Where(p => p.ProductType == productType)
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }

        public async Task<List<Product>> GetFoodAndBeverageAsync()
        {
            return await _context.Products
                .Where(p => p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE)
                .OrderBy(p => p.ProductType)
                .ThenBy(p => p.ProductName)
                .ToListAsync();
        }

        public async Task<List<Product>> GetEquipmentAndSuppliesAsync()
        {
            return await _context.Products
                .Where(p => p.ProductType == ProductType.EQUIPMENT || p.ProductType == ProductType.SUPPLIES)
                .OrderBy(p => p.ProductType)
                .ThenBy(p => p.ProductName)
                .ToListAsync();
        }

        public async Task<List<Product>> GetAvailableAsync()
        {
            return await _context.Products
                .Where(p => p.Status == "AVAILABLE" && p.Quantity > 0)
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }

        public async Task<List<Product>> GetAvailableFoodAndBeverageAsync()
        {
            return await _context.Products
                .Where(p => (p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE) 
                          && p.Status == "AVAILABLE" && p.Quantity > 0)
                .OrderBy(p => p.ProductType)
                .ThenBy(p => p.ProductName)
                .ToListAsync();
        }

        public async Task<List<Product>> GetByStatusAsync(string status)
        {
            return await _context.Products
                .Where(p => p.Status == status)
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Products.CountAsync();
        }

        public async Task<int> GetCountByTypeAsync(ProductType productType)
        {
            return await _context.Products
                .CountAsync(p => p.ProductType == productType);
        }

        public async Task<int> GetLowStockCountAsync(int threshold = 10)
        {
            return await _context.Products
                .CountAsync(p => p.Quantity < threshold);
        }

        public async Task<int> GetLowStockCountByTypeAsync(ProductType productType, int threshold = 10)
        {
            return await _context.Products
                .CountAsync(p => p.ProductType == productType && p.Quantity < threshold);
        }

        public async Task<decimal> GetTotalValueAsync()
        {
            return await _context.Products
                .SumAsync(p => (p.Price ?? 0) * (p.Quantity ?? 0));
        }

        public async Task<decimal> GetTotalValueByTypeAsync(ProductType productType)
        {
            return await _context.Products
                .Where(p => p.ProductType == productType)
                .SumAsync(p => (p.Price ?? 0) * (p.Quantity ?? 0));
        }

        public async Task<Product> CreateAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<Product> UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
} 