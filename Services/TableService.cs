using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;

namespace BilliardsManagement.Services
{
    public class TableService : ITableService
    {
        private readonly BilliardsDbContext _context;

        public TableService(BilliardsDbContext context)
        {
            _context = context;
        }

        public async Task<List<Table>> GetAllTablesAsync()
        {
            return await _context.Tables.ToListAsync();
        }

        public async Task<Table?> GetTableByIdAsync(int tableId)
        {
            return await _context.Tables.FindAsync(tableId);
        }

        public async Task<Table> AddTableAsync()
        {
            var maxTableId = await _context.Tables.MaxAsync(t => (int?)t.TableId) ?? 0;
            var nextTableNumber = maxTableId + 1;

            var newTable = new Table
            {
                TableName = $"Bàn {nextTableNumber}",
                TableType = "STANDARD",
                Status = "AVAILABLE",
                PricePerHour = 100000 // Default price 100,000 VND
            };

            _context.Tables.Add(newTable);
            await _context.SaveChangesAsync();

            return newTable;
        }

        public async Task<bool> UpdateTableAsync(int tableId, string tableType, string status, decimal pricePerHour)
        {
            var table = await _context.Tables.FindAsync(tableId);
            if (table == null)
                return false;

            table.TableType = tableType;
            table.Status = status;
            // Automatically set price based on table type
            table.PricePerHour = tableType?.ToUpper() == "VIP" ? 200000 : 100000;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleTableStatusAsync(int tableId)
        {
            var table = await _context.Tables.FindAsync(tableId);
            if (table == null)
                return false;

            // Update status rotation: AVAILABLE -> MAINTENANCE -> BROKEN -> AVAILABLE
            table.Status = table.Status switch
            {
                "AVAILABLE" => "MAINTENANCE",
                "MAINTENANCE" => "BROKEN",
                _ => "AVAILABLE"
            };

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Table>> GetOccupiedTablesWithSessionsAsync()
        {
            return await _context.Tables
                .Include(t => t.Sessions.Where(s => s.EndTime == null))
                .ThenInclude(s => s.Orders)
                .ThenInclude(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(t => t.Sessions.Any(s => s.EndTime == null))
                .ToListAsync();
        }

        public async Task<List<Table>> GetAvailableTablesAsync()
        {
            return await _context.Tables
                .Where(t => t.Status == "AVAILABLE" && !t.Sessions.Any(s => s.EndTime == null))
                .ToListAsync();
        }

        public async Task<bool> TransferTableAsync(int fromTableId, int toTableId, int employeeId)
        {
            try
            {
                // Validate tables
                var fromTable = await _context.Tables
                    .Include(t => t.Sessions.Where(s => s.EndTime == null))
                    .ThenInclude(s => s.Orders)
                    .ThenInclude(o => o.OrderDetails)
                    .FirstOrDefaultAsync(t => t.TableId == fromTableId);

                var toTable = await _context.Tables.FindAsync(toTableId);

                if (fromTable == null || toTable == null)
                    return false;

                // Check if source table has active session
                var activeSession = fromTable.Sessions.FirstOrDefault(s => s.EndTime == null);
                if (activeSession == null)
                    return false;

                // Check if destination table is available
                var hasActiveSessionInToTable = await _context.Sessions
                    .AnyAsync(s => s.TableId == toTableId && s.EndTime == null);

                if (hasActiveSessionInToTable || toTable.Status != "AVAILABLE")
                    return false;

                // Transfer session to new table
                activeSession.TableId = toTableId;

                // Update table statuses
                fromTable.Status = "AVAILABLE";
                toTable.Status = "OCCUPIED";

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<dynamic?> GetTableTransferInfoAsync(int tableId)
        {
            try
            {
                var table = await _context.Tables
                    .Include(t => t.Sessions.Where(s => s.EndTime == null))
                    .ThenInclude(s => s.Orders)
                    .ThenInclude(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(t => t.TableId == tableId);

                if (table == null)
                    return null;

                var activeSession = table.Sessions.FirstOrDefault(s => s.EndTime == null);
                if (activeSession == null)
                    return null;

                // Calculate current totals
                var duration = DateTime.Now - (activeSession.StartTime ?? DateTime.Now);
                var tableTotal = table.PricePerHour.HasValue 
                    ? (decimal)duration.TotalHours * table.PricePerHour.Value 
                    : 0;

                var serviceTotal = activeSession.Orders?
                    .SelectMany(o => o.OrderDetails ?? new List<OrderDetail>())
                    .Sum(od => (od.UnitPrice ?? 0) * (od.Quantity ?? 0)) ?? 0;

                var orderDetailsList = activeSession.Orders?
                    .SelectMany(o => o.OrderDetails ?? new List<OrderDetail>())
                    .Select(od => new {
                        productName = od.Product?.ProductName ?? "N/A",
                        quantity = od.Quantity ?? 0,
                        unitPrice = od.UnitPrice ?? 0,
                        total = (od.UnitPrice ?? 0) * (od.Quantity ?? 0)
                    }).ToList();

                return new {
                    tableId = table.TableId,
                    tableName = table.TableName,
                    tableType = table.TableType,
                    pricePerHour = table.PricePerHour,
                    sessionId = activeSession.SessionId,
                    startTime = activeSession.StartTime?.ToString("dd/MM/yyyy HH:mm:ss"),
                    duration = $"{duration.Hours:00}:{duration.Minutes:00}:{duration.Seconds:00}",
                    tableTotal = tableTotal,
                    serviceTotal = serviceTotal,
                    grandTotal = tableTotal + serviceTotal,
                    orderDetails = orderDetailsList
                };
            }
            catch
            {
                return null;
            }
        }
    }
} 