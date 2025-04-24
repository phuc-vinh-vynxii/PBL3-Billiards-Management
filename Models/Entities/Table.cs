using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BilliardsManagement.Models.Entities;

[Table("Table")]
public partial class Table
{
    public int TableId { get; set; }

    public string? TableName { get; set; }

    public string? TableType { get; set; }

    public string? Status { get; set; }

    public decimal? PricePerHour { get; set; }

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
