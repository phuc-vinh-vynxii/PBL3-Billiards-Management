using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Models.ViewModels
{
    public class TableInfoViewModel
    {
        public int TableId { get; set; }
        public string? TableName { get; set; }
        public string? TableType { get; set; }
        public string? Status { get; set; }
        public decimal? PricePerHour { get; set; }

        // For active session
        public int? SessionId { get; set; }
        public DateTime? StartTime { get; set; }
        public decimal? TableTotal { get; set; }
        public decimal? TotalTime { get; set; }
        public decimal? TotalAmount { get; set; } // Total of table price + products

        // Employee ID who starts the session
        public int? EmployeeId { get; set; }

        // For orders and products
        public List<Order> Orders { get; set; } = new List<Order>();
        public List<Product> AvailableProducts { get; set; } = new List<Product>();
    }
}