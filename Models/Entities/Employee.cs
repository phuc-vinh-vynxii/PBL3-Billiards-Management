using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BilliardsManagement.Models.Entities;

[Table("Employee")]
public partial class Employee
{
    [Key]
    public int EmployeeId { get; set; }

    [Required]
    [StringLength(50)]
    public string? FullName { get; set; }

    [Required]
    [StringLength(15)]
    public string? Position { get; set; }

    [StringLength(15)]
    [Phone]
    public string? Phone { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [StringLength(50)]
    public string? Username { get; set; }

    [Required]
    [StringLength(255)]
    public string? Password { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual ICollection<EmployeePermission> EmployeePermissions { get; set; } = new List<EmployeePermission>();

    public virtual ICollection<EmployeePermission> GrantedPermissions { get; set; } = new List<EmployeePermission>();
}
