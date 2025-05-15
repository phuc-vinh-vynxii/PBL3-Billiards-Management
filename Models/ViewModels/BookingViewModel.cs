using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Models.ViewModels;

public class BookingViewModel
{
    public IEnumerable<Table> Tables { get; set; } = new List<Table>();
    public IEnumerable<Customer> Customers { get; set; } = new List<Customer>();
    public IEnumerable<Reservation> Reservations { get; set; } = new List<Reservation>();
    
    // Form fields for creating a new reservation
    public int SelectedTableId { get; set; }
    public int SelectedCustomerId { get; set; }
    public DateTime ReservationDate { get; set; } = DateTime.Now;
    public TimeSpan StartTime { get; set; } = TimeSpan.FromHours(8); // 8:00 AM default
    public TimeSpan EndTime { get; set; } = TimeSpan.FromHours(10); // 10:00 AM default
    public string Notes { get; set; } = string.Empty;
} 