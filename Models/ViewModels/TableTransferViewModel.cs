using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Models.ViewModels
{
    public class TableTransferViewModel
    {
        public List<Table> OccupiedTables { get; set; } = new List<Table>();
        public List<Table> AvailableTables { get; set; } = new List<Table>();
    }
} 