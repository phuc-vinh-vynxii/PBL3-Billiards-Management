using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Repositories
{
    public class TableRepository : ITableRepository
    {
        private readonly BilliardsDbContext _context;

        public TableRepository(BilliardsDbContext context)
        {
            _context = context;
        }

        public async Task<Table?> GetByIdAsync(int id)
        {
            return await _context.Tables.FindAsync(id);
        }

        public async Task<List<Table>> GetAllAsync()
        {
            return await _context.Tables
                .OrderBy(t => t.TableId)
                .ToListAsync();
        }

        public async Task<List<Table>> GetWithActiveSessionsAsync()
        {
            return await _context.Tables
                .Include(t => t.Sessions.Where(s => s.EndTime == null))
                .OrderBy(t => t.TableId)
                .ToListAsync();
        }

        public async Task<List<Table>> GetByStatusAsync(string status)
        {
            return await _context.Tables
                .Where(t => t.Status == status)
                .OrderBy(t => t.TableId)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Tables.CountAsync();
        }

        public async Task<int> GetCountByStatusAsync(string status)
        {
            return await _context.Tables.CountAsync(t => t.Status == status);
        }

        public async Task<Table> CreateAsync(Table table)
        {
            _context.Tables.Add(table);
            await _context.SaveChangesAsync();
            return table;
        }

        public async Task<Table> UpdateAsync(Table table)
        {
            _context.Tables.Update(table);
            await _context.SaveChangesAsync();
            return table;
        }

        public async Task DeleteAsync(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table != null)
            {
                _context.Tables.Remove(table);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
} 