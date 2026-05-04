namespace SalonBooker.Services.Interfaces
{
    public interface IAppointmentService
    {
        Task<List<string>> GetAvailableSlotsAsync(int barberId, int serviceId, DateTime date);
        Task<bool> IsSlotFreeAsync(int barberId, DateTime startTime, DateTime endTime);
    }
}