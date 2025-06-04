using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Services
{
    public interface ISessionService
    {
        Task<(bool Success, string Message, Session? Session)> StartSessionAsync(int tableId, int employeeId, int? customerId = null, int? reservationId = null);
        Task<(bool Success, string Message)> EndSessionAsync(int sessionId);
        Task<Session?> GetActiveSessionByTableIdAsync(int tableId);
        Task<Session?> GetSessionByIdAsync(int sessionId);
        Task<List<Session>> GetActiveSessionsAsync();
        Task<bool> IsTableInUseAsync(int tableId);
        Task<int?> ResolveEmployeeIdAsync(int? employeeId, string? username, string? role);
    }
} 