using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BilliardsManagement.Models.Entities;

public class Table
{
    [Key]
    public int TableId { get; set; }

    [StringLength(50)]
    public string? TableName { get; set; }

    [StringLength(10)]
    public string? TableType { get; set; }

    [StringLength(15)]
    public string? Status { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? PricePerHour { get; set; }

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
