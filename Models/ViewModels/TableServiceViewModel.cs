using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Models.ViewModels
{
    public class TableServiceViewModel
    {
        public Table Table { get; set; }
        public Session Session { get; set; }
        public List<Product> AvailableProducts { get; set; } = new List<Product>();
        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public decimal OrderTotal { get; set; }
        public Dictionary<int, int> ProductAvailability { get; set; } = new Dictionary<int, int>();
    }
} 