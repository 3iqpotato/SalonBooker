using Microsoft.EntityFrameworkCore;
using SalonBooker.Data;
using SalonBooker.Services.Interfaces;

namespace SalonBooker.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;

        public AppointmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<string>> GetAvailableSlotsAsync(int barberId, int serviceId, DateTime date)
        {
            var service = await _context.Services.FindAsync(serviceId);
            if (service == null) return new List<string>();

            var barber = await _context.Barbers.FindAsync(barberId);
            if (barber == null) return new List<string>();

            var workStart = barber.WorkStartTime.ToTimeSpan();
            var workEnd = barber.WorkEndTime.ToTimeSpan();

            // Генерираме всички слотове
            var allSlots = new List<TimeSpan>();
            var currentTime = workStart;

            while (currentTime + TimeSpan.FromMinutes(service.DurationMinutes) <= workEnd)
            {
                allSlots.Add(currentTime);
                currentTime = currentTime.Add(TimeSpan.FromMinutes(barber.SlotDurationMinutes));
            }

            // Ако е днес — филтрираме минали часове
            if (date.Date == DateTime.Today)
            {
                var nowTime = DateTime.Now.TimeOfDay.Add(TimeSpan.FromMinutes(15));
                allSlots = allSlots.Where(s => s > nowTime).ToList();
            }

            // Вземаме заетите резервации за деня
            var bookedAppointments = await _context.Appointments
                .Where(a => a.BarberId == barberId
                    && a.StartTime >= date.Date
                    && a.StartTime < date.Date.AddDays(1))
                .Select(a => new { a.StartTime, a.EndTime })
                .ToListAsync();

            // Филтрираме свободните
            var freeSlots = new List<string>();

            foreach (var slot in allSlots)
            {
                var slotStart = date.Date + slot;
                var slotEnd = slotStart.Add(TimeSpan.FromMinutes(service.DurationMinutes));
                bool isFree = true;

                foreach (var booked in bookedAppointments)
                {
                    if (slotStart < booked.EndTime && slotEnd > booked.StartTime)
                    {
                        isFree = false;
                        break;
                    }
                }

                if (isFree)
                    freeSlots.Add(slot.ToString(@"hh\:mm"));
            }

            return freeSlots;
        }

        public async Task<bool> IsSlotFreeAsync(int barberId, DateTime startTime, DateTime endTime)
        {
            return !await _context.Appointments.AnyAsync(a =>
                a.BarberId == barberId &&
                a.StartTime < endTime &&
                a.EndTime > startTime);
        }
    }
}