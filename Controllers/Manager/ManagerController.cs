using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;

namespace BilliardsManagement.Controllers
{
    public class ManagerController : Controller
    {
        private readonly BilliardsDbContext _context;

        public ManagerController(BilliardsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Check if user is manager
            var role = HttpContext.Session.GetString("Role")?.ToUpper();
            if (role != "MANAGER")
            {
                return RedirectToAction("Login", "Account");
            }

            var today = DateTime.Today;
            var model = new ManagerDashboardViewModel
            {
                Tables = await _context.Tables.ToListAsync(),
                TotalTables = await _context.Tables.CountAsync(),
                AvailableTables = await _context.Tables.CountAsync(t => t.Status == "AVAILABLE"),
                BrokenTables = await _context.Tables.CountAsync(t => t.Status == "BROKEN"),
                MaintenanceTables = await _context.Tables.CountAsync(t => t.Status == "MAINTENANCE"),
                TotalEmployees = await _context.Employees.CountAsync(),
                TotalProducts = await _context.Products.CountAsync(),
                LowStockProducts = await _context.Products.CountAsync(p => p.Quantity < 10),
                TodayRevenue = await _context.Invoices
                    .Where(i => i.PaymentTime!.Value.Date == today)
                    .SumAsync(i => i.TotalAmount ?? 0)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> TableManagement()
        {
            var tables = await _context.Tables.ToListAsync();
            return View(tables);
        }

        [HttpPost]
        public async Task<IActionResult> AddTable()
        {
            var maxTableId = await _context.Tables.MaxAsync(t => (int?)t.TableId) ?? 0;
            var nextTableNumber = maxTableId + 1;

            var newTable = new Table
            {
                TableName = $"Bàn {nextTableNumber}",
                TableType = "STANDARD",
                Status = "AVAILABLE",
                PricePerHour = 100000 // Default price 100,000 VND
            };

            _context.Tables.Add(newTable);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(TableManagement));
        }

        [HttpPost]
        public async Task<IActionResult> EditTable(int tableId, string tableType, string status, decimal pricePerHour)
        {
            var table = await _context.Tables.FindAsync(tableId);
            if (table == null)
                return NotFound();

            table.TableType = tableType;
            table.Status = status;
            // Automatically set price based on table type
            table.PricePerHour = tableType?.ToUpper() == "VIP" ? 200000 : 100000;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(TableManagement));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleTableStatus(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null)
                return NotFound();

            // Update status rotation: AVAILABLE -> MAINTENANCE -> BROKEN -> AVAILABLE
            table.Status = table.Status switch
            {
                "AVAILABLE" => "MAINTENANCE",
                "MAINTENANCE" => "BROKEN",
                _ => "AVAILABLE"
            };

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(TableManagement));
        }

        public IActionResult Revenue()
        {
            return View();
        }

        public IActionResult Inventory()
        {
            return View();
        }

        public IActionResult Staff()
        {
            return View();
        }

        public IActionResult Roles()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Booking()
        {
            var tables = await _context.Tables
                .Include(t => t.Sessions.Where(s => s.EndTime == null))
                .ToListAsync();
            return View(tables);
        }

        [HttpGet]
        public async Task<IActionResult> FoodAndBeverage()
        {
            var model = new InventoryViewModel
            {
                Products = await _context.Products
                    .Where(p => p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE)
                    .OrderBy(p => p.ProductType)
                    .ToListAsync(),
                TotalValue = await _context.Products
                    .Where(p => p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE)
                    .SumAsync(p => (p.Price ?? 0) * (p.Quantity ?? 0)),
                TotalItems = await _context.Products
                    .CountAsync(p => p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE),
                LowStockItems = await _context.Products
                    .CountAsync(p => (p.ProductType == ProductType.FOOD || p.ProductType == ProductType.BEVERAGE) 
                                   && p.Quantity < 10)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Equipment()
        {
            var model = new InventoryViewModel
            {
                Products = await _context.Products
                    .Where(p => p.ProductType == ProductType.EQUIPMENT || p.ProductType == ProductType.SUPPLIES)
                    .ToListAsync(),
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

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            return Json(new
            {
                productId = product.ProductId,
                productName = product.ProductName,
                productType = product.ProductType.ToString(),
                price = product.Price,
                quantity = product.Quantity,
                status = product.Status
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveProduct(Product product)
        {
            if (product.ProductId == 0)
            {
                _context.Products.Add(product);
            }
            else
            {
                var existingProduct = await _context.Products.FindAsync(product.ProductId);
                if (existingProduct == null)
                    return NotFound();

                existingProduct.ProductName = product.ProductName;
                existingProduct.ProductType = product.ProductType;
                existingProduct.Price = product.Price;
                existingProduct.Quantity = product.Quantity;
                existingProduct.Status = product.Status;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(product.ProductType == ProductType.FOOD || product.ProductType == ProductType.BEVERAGE 
                ? nameof(FoodAndBeverage) 
                : nameof(Equipment));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}