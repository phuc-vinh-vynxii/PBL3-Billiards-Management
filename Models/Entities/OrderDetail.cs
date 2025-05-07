using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BilliardsManagement.Models.Entities;

[Table("OrderDetail")]
public partial class OrderDetail
{
    [Key]
    public int OrderDetailId { get; set; }

    public int? OrderId { get; set; }

    public int? ProductId { get; set; }

    public int? Quantity { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? UnitPrice { get; set; }

    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }
}
