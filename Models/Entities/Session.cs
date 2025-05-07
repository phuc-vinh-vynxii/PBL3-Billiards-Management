using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BilliardsManagement.Models.Entities;

public class Session
{
    [Key]
    public int SessionId { get; set; }

    public int? TableId { get; set; }

    public int? EmployeeId { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? TableTotal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? TotalTime { get; set; }

    [ForeignKey("EmployeeId")]
    public virtual Employee? Employee { get; set; }

    [ForeignKey("TableId")]
    public virtual Table? Table { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
