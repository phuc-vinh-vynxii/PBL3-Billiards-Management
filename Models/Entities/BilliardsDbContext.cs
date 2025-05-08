using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BilliardsManagement.Models.Entities;

public partial class BilliardsDbContext : DbContext
{
    // private readonly IConfiguration? _configuration;
    // public BilliardsDbContext(IConfiguration configuration)
    // {
    //     _configuration = configuration;
    // }
    public BilliardsDbContext(DbContextOptions<BilliardsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<Table> Tables { get; set; }

    // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // {
    //     // if (_configuration == null)
    //     // {
    //     //     throw new InvalidOperationException("Configuration is not initialized.");
    //     // }
    //     var connectionString = .GetConnectionString("BilliardsDbConnection");
    //         optionsBuilder.UseSqlServer(connectionString);
    // }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(e => e.CustomerId).ValueGeneratedOnAdd();
            entity.Property(e => e.Phone).HasMaxLength(15);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(e => e.EmployeeId).ValueGeneratedOnAdd();
            entity.Property(e => e.Phone).HasMaxLength(15);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(50);
            entity.Property(e => e.Position).HasMaxLength(15);
            entity.Property(e => e.Username).HasMaxLength(50);
            entity.Property(e => e.Password).HasMaxLength(255);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.Property(e => e.InvoiceId).ValueGeneratedOnAdd();
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(15,2)");
            entity.Property(e => e.Discount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.PaymentMethod).HasMaxLength(15);
            entity.Property(e => e.Status).HasMaxLength(10);

            entity.HasOne(d => d.Cashier)
                .WithMany(p => p.Invoices)
                .HasForeignKey(d => d.CashierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Session)
                .WithMany(p => p.Invoices)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.OrderId).ValueGeneratedOnAdd();
            entity.Property(e => e.Status).HasMaxLength(15);

            entity.HasOne(d => d.Session)
                .WithMany(p => p.Orders)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.OrderNavigation)
                .WithOne(p => p.Order)
                .HasForeignKey<Order>(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.Property(e => e.OrderDetailId).ValueGeneratedOnAdd();
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10,2)");

            entity.HasOne(d => d.Order)
                .WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Product)
                .WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.ProductId).ValueGeneratedOnAdd();
            entity.Property(e => e.ProductName).HasMaxLength(100);
            entity.Property(e => e.Category).HasMaxLength(10);
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Status).HasMaxLength(15);

            // Configure ProductType enum to be stored as string
            entity.Property(e => e.ProductType)
                .HasConversion<string>();
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.Property(e => e.ReservationId).ValueGeneratedOnAdd();
            entity.Property(e => e.Status).HasMaxLength(15);
            entity.Property(e => e.Notes).HasColumnType("text");

            entity.HasOne(d => d.Customer)
                .WithMany(p => p.Reservations)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Table)
                .WithMany(p => p.Reservations)
                .HasForeignKey(d => d.TableId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.Property(e => e.SessionId).ValueGeneratedOnAdd();
            entity.Property(e => e.TableTotal).HasColumnType("decimal(10,2)");
            entity.Property(e => e.TotalTime).HasColumnType("decimal(10,2)");

            entity.HasOne(d => d.Employee)
                .WithMany(p => p.Sessions)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Table)
                .WithMany(p => p.Sessions)
                .HasForeignKey(d => d.TableId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Table>(entity =>
        {
            entity.Property(e => e.TableId).ValueGeneratedOnAdd();
            entity.Property(e => e.TableName).HasMaxLength(50);
            entity.Property(e => e.TableType).HasMaxLength(10);
            entity.Property(e => e.Status).HasMaxLength(15);
            entity.Property(e => e.PricePerHour).HasColumnType("decimal(10,2)");
        });
    }
}
