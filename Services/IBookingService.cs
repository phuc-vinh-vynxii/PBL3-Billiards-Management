using BilliardsManagement.Models.Entities;
using BilliardsManagement.Models.ViewModels;

namespace BilliardsManagement.Services
{
    public interface IBookingService
    {
        Task<BookingViewModel> GetBookingDataAsync();
        Task UpdateExpiredReservationsAsync();
        Task<(bool Success, string Message)> CreateReservationAsync(int tableId, int customerId, string bookingDate, string startTimeStr, string endTimeStr, string notes);
        Task<(bool Success, string Message)> CancelReservationAsync(int reservationId);
        Task<(bool Success, string Message)> StartSessionAsync(int tableId, int employeeId, int? customerId = null, int? reservationId = null);
        Task<(bool Success, string Message)> EndSessionAsync(int sessionId);
        Task<List<dynamic>> GetUpcomingReservationsAsync();
    }
} 