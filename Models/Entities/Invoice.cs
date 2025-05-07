using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BilliardsManagement.Models.Entities;

[Table("Invoice")]
public partial class Invoice
{
    [Key]
    public int InvoiceId { get; set; }

    public int? SessionId { get; set; }

    public int? CashierId { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal? TotalAmount { get; set; }

    public DateTime? PaymentTime { get; set; }

    [StringLength(15)]
    public string? PaymentMethod { get; set; }

    [StringLength(10)]
    public string? Status { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Discount { get; set; }

    [ForeignKey("CashierId")]
    public virtual Employee? Cashier { get; set; }

    [ForeignKey("SessionId")]
    public virtual Session? Session { get; set; }
}
