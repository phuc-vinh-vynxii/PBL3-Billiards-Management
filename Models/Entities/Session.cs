using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BilliardsManagement.Models.Entities;

[Table("Session")]
public partial class Session
{
    public int SessionId { get; set; }

    public int? TableId { get; set; }

    public int? EmployeeId { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public decimal? TotalTime { get; set; }

    public decimal? TableTotal { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Table? Table { get; set; }
}
