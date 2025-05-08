using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Models.ViewModels;

public class InventoryViewModel
{
    public IEnumerable<Product>? Products { get; set; }
    public string? CurrentTab { get; set; }
    public decimal TotalValue { get; set; }
    public int TotalItems { get; set; }
    public int LowStockItems { get; set; }
}