using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BilliardsManagement.Models.Entities;

[Table("Reservation")]
public partial class Reservation
{
    [Key]
    public int ReservationId { get; set; }

    public int? CustomerId { get; set; }

    public int? TableId { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    [StringLength(15)]
    public string? Status { get; set; }

    [Column(TypeName = "text")]
    public string? Notes { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [ForeignKey("TableId")]
    public virtual Table? Table { get; set; }
}
