using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BilliardsManagement.Models.Entities;

[Table("Product")]
public partial class Product
{
    [Key]
    public int ProductId { get; set; }

    [StringLength(100)]
    public string? ProductName { get; set; }

    [StringLength(10)]
    public string? Category { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Price { get; set; }

    [StringLength(15)]
    public string? Status { get; set; }

    public int? Quantity { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
