using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BilliardsManagement.Models.Entities;

[Table("Customer")]

public partial class Customer
{
    [Key]
    public int CustomerId { get; set; }

    [StringLength(15)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? FullName { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    public int? LoyaltyPoints { get; set; }

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
