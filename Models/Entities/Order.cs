using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BilliardsManagement.Models.Entities;

[Table("Orders")]
public partial class Order
{
    [Key]
    public int OrderId { get; set; }

    public int? SessionId { get; set; }

    public int? EmployeeId { get; set; }

    public DateTime? CreatedAt { get; set; }

    [StringLength(15)]
    public string? Status { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    [ForeignKey("EmployeeId")]
    public virtual Employee OrderNavigation { get; set; } = null!;

    [ForeignKey("SessionId")]
    public virtual Session? Session { get; set; }
}
