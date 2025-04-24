using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BilliardsManagement.Models.Entities;

[Table("Invoice")]
public partial class Invoice
{
    public int InvoiceId { get; set; }

    public int? SessionId { get; set; }

    public int? CashierId { get; set; }

    public decimal? TotalAmount { get; set; }

    public DateTime? PaymentTime { get; set; }

    public string? PaymentMethod { get; set; }

    public string? Status { get; set; }

    public decimal? Discount { get; set; }

    public virtual Employee? Cashier { get; set; }

    public virtual Session? Session { get; set; }
}
