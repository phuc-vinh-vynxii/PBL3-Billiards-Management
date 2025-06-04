using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;

namespace BilliardsManagement.Services
{
    public class ProductService : IProductService
    {
        private readonly BilliardsDbContext _context;

        public ProductService(BilliardsDbContext context)
        {
            _context = context;
        }

        public async Task<InventoryViewModel> GetFoodAndBeverageInventoryAsync()
        {
            var products = await _context.Products
                .Where(p => p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE)
                .OrderBy(p => p.ProductType)
                .ToListAsync();

            return new InventoryViewModel
            {
                Products = products,
                TotalValue = await _context.Products
                    .Where(p => p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE)
                    .SumAsync(p => (p.Price ?? 0) * (p.Quantity ?? 0)),
                TotalItems = await _context.Products
                    .CountAsync(p => p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE),
                LowStockItems = await _context.Products
                    .CountAsync(p => (p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE) 
                                   && p.Quantity < 10)
            };
        }

        public async Task<InventoryViewModel> GetEquipmentInventoryAsync()
        {
            var products = await _context.Products
                .Where(p => p.ProductType == ProductType.EQUIPMENT || p.ProductType == ProductType.SUPPLIES)
                .ToListAsync();

            return new InventoryViewModel
            {
                Products = products,
                CurrentTab = "EQUIPMENT",
                TotalValue = await _context.Products
                    .Where(p => p.ProductType == ProductType.EQUIPMENT || p.ProductType == ProductType.SUPPLIES)
                    .SumAsync(p => (p.Price ?? 0) * (p.Quantity ?? 0)),
                TotalItems = await _context.Products
                    .CountAsync(p => p.ProductType == ProductType.EQUIPMENT || p.ProductType == ProductType.SUPPLIES),
                LowStockItems = await _context.Products
                    .CountAsync(p => (p.ProductType == ProductType.EQUIPMENT || p.ProductType == ProductType.SUPPLIES) 
                                   && p.Quantity < 10)
            };
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<bool> SaveProductAsync(Product product)
        {
            try
            {
                if (product.ProductId == 0)
                {
                    _context.Products.Add(product);
                }
                else
                {
                    var existingProduct = await _context.Products.FindAsync(product.ProductId);
                    if (existingProduct == null)
                        return false;

                    existingProduct.ProductName = product.ProductName;
                    existingProduct.ProductType = product.ProductType;
                    existingProduct.Price = product.Price;
                    existingProduct.Quantity = product.Quantity;
                    existingProduct.Status = product.Status;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return false;

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<dynamic>> GetAvailableProductsAsync()
        {
            var products = await _context.Products
                .Where(p => (p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE) 
                          && p.Status == "AVAILABLE" && p.Quantity > 0)
                .Select(p => new {
                    productId = p.ProductId,
                    productName = p.ProductName,
                    productType = p.ProductType.ToString(),
                    price = p.Price,
                    quantity = p.Quantity
                })
                .ToListAsync();
                
            return products.Cast<dynamic>().ToList();
        }
    }
} 