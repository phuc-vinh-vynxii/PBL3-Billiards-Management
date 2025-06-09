using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Repositories
{
    public interface ITableRepository
    {
        Task<Table?> GetByIdAsync(int id);
        Task<List<Table>> GetAllAsync();
        Task<List<Table>> GetWithActiveSessionsAsync();
        Task<List<Table>> GetByStatusAsync(string status);
        Task<int> GetCountAsync();
        Task<int> GetCountByStatusAsync(string status);
        Task<Table> CreateAsync(Table table);
        Task<Table> UpdateAsync(Table table);
        Task DeleteAsync(int id);
        Task SaveChangesAsync();
    }
} 