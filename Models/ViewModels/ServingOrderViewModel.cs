using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Models.ViewModels;

public class ServingOrderViewModel
{
    public int TableId { get; set; }
    public string? TableName { get; set; }
    public IEnumerable<Product>? AvailableProducts { get; set; }
    public IEnumerable<OrderDetail>? CurrentOrders { get; set; }
    public decimal? TotalAmount { get; set; }
    public int? SessionId { get; set; }
}