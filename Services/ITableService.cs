using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;

namespace BilliardsManagement.Services
{
    public interface ITableService
    {
        Task<List<Table>> GetAllTablesAsync();
        Task<Table?> GetTableByIdAsync(int tableId);
        Task<Table> AddTableAsync();
        Task<bool> UpdateTableAsync(int tableId, string tableType, string status, decimal pricePerHour);
        Task<bool> ToggleTableStatusAsync(int tableId);
        Task<List<Table>> GetOccupiedTablesWithSessionsAsync();
        Task<List<Table>> GetAvailableTablesAsync();
        Task<bool> TransferTableAsync(int fromTableId, int toTableId, int employeeId);
        Task<dynamic?> GetTableTransferInfoAsync(int tableId);
    }
} 