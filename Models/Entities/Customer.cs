using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BilliardsManagement.Models.Entities;

[Table("Customer")]

public partial class Customer
{
    public int CustomerId { get; set; }

    public string? Phone { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public int? LoyaltyPoints { get; set; }

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
