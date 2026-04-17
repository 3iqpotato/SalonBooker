using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SalonBooker.Data;
using SalonBooker.Models;
using Microsoft.AspNetCore.Identity;

namespace SalonBooker.Pages.Appointments
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public int SelectedBarberId { get; set; }

        [BindProperty]
        public int SelectedServiceId { get; set; }

        [BindProperty]
        public string SelectedDate { get; set; } = string.Empty;

        [BindProperty]
        public string SelectedTime { get; set; } = string.Empty;

        public List<SelectListItem> Barbers { get; set; } = new();

        public class ServiceSelectItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public int DurationMinutes { get; set; }
        }

        public class LoadServicesRequest
        {
            public int BarberId { get; set; }
        }

        public class LoadSlotsRequest
        {
            public int BarberId { get; set; }
            public int ServiceId { get; set; }
            public DateTime Date { get; set; }
        }

        public async Task OnGetAsync()
        {
            Barbers = await _context.Barbers
                .Include(b => b.User)
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.User.FullName ?? b.User.UserName
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostLoadServicesAsync([FromBody] LoadServicesRequest request)
        {
            var services = await _context.BarberServices
                .Where(bs => bs.BarberId == request.BarberId)
                .Include(bs => bs.Service)
                .Select(bs => new ServiceSelectItem
                {
                    Id = bs.Service.Id,
                    Name = bs.Service.Name,
                    Price = bs.Service.Price,
                    DurationMinutes = bs.Service.DurationMinutes
                })
                .ToListAsync();

            return new JsonResult(services);
        }

        public async Task<IActionResult> OnPostLoadAvailableSlotsAsync([FromBody] LoadSlotsRequest request)
        {
            var service = await _context.Services.FindAsync(request.ServiceId);
            if (service == null)
                return new JsonResult(new List<string>());

            var barber = await _context.Barbers.FindAsync(request.BarberId);
            if (barber == null)
                return new JsonResult(new List<string>());

            var workStart = barber.WorkStartTime.ToTimeSpan();
            var workEnd = barber.WorkEndTime.ToTimeSpan();
            var slotDuration = barber.SlotDurationMinutes;

            // Генерираме слотове - слотът трябва да свършва преди края на работния ден
            var allSlots = new List<TimeSpan>();
            var currentTime = workStart;

            while (currentTime + TimeSpan.FromMinutes(service.DurationMinutes) <= workEnd)
            {
                allSlots.Add(currentTime);
                currentTime = currentTime.Add(TimeSpan.FromMinutes(slotDuration));
            }

            // Ако датата е днес - филтрираме минали часове
            if (request.Date.Date == DateTime.Today)
            {
                var nowTime = DateTime.Now.TimeOfDay.Add(TimeSpan.FromMinutes(15)); // 15 мин. буфер
                allSlots = allSlots.Where(s => s > nowTime).ToList();
            }

            var dateStart = request.Date.Date;
            var dateEnd = request.Date.Date.AddDays(1);

            var bookedAppointments = await _context.Appointments
                .Where(a => a.BarberId == request.BarberId
                    && a.StartTime >= dateStart
                    && a.StartTime < dateEnd)
                .Select(a => new { a.StartTime, a.EndTime })
                .ToListAsync();

            var freeSlots = new List<string>();

            foreach (var slot in allSlots)
            {
                var slotStart = request.Date.Date + slot;
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
                {
                    freeSlots.Add(slot.ToString(@"hh\:mm"));
                }
            }

            return new JsonResult(freeSlots);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login");

            // Проверка дали потребителят е активен
            if (!user.IsActive)
            {
                TempData["ErrorMessage"] = "Акаунтът ви е блокиран. Не можете да правите резервации.";
                return RedirectToPage("/Index");
            }

            // Проверка за точки - трябват поне 1 точка
            if (user.Points <= 0)
            {
                TempData["ErrorMessage"] = "Нямате достатъчно точки за нова резервация.";
                return RedirectToPage("/Index");
            }

            // Проверка за максимален брой активни резервации (2)
            var activeBookings = await _context.Appointments
                .CountAsync(a => a.ClientUserId == user.Id
                    && !a.IsCompleted
                    && a.StartTime > DateTime.Now);

            if (activeBookings >= 2)
            {
                TempData["ErrorMessage"] = "Можете да имате максимум 2 активни резервации.";
                return RedirectToPage("/Index");
            }

            // Парсване на дата и час
            if (!DateTime.TryParse(SelectedDate, out var parsedDate))
            {
                TempData["ErrorMessage"] = "Невалидна дата.";
                return RedirectToPage();
            }

            if (!TimeSpan.TryParse(SelectedTime, out var parsedTime))
            {
                TempData["ErrorMessage"] = "Невалиден час.";
                return RedirectToPage();
            }

            var service = await _context.Services.FindAsync(SelectedServiceId);
            var barber = await _context.Barbers.FindAsync(SelectedBarberId);

            if (service == null || barber == null)
            {
                TempData["ErrorMessage"] = "Невалидни данни.";
                return RedirectToPage("/Index");
            }

            var startTime = parsedDate.Date + parsedTime;
            var endTime = startTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));

            // Проверка дали часът е в миналото
            if (startTime < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Не можете да резервирате час в миналото.";
                return RedirectToPage();
            }

            // Проверка дали часът свършва преди края на работния ден
            if (endTime.TimeOfDay > barber.WorkEndTime.ToTimeSpan())
            {
                TempData["ErrorMessage"] = "Избраният час излиза извън работното време на фризьора.";
                return RedirectToPage();
            }

            // Проверка дали часът все още е свободен
            var isStillFree = !await _context.Appointments.AnyAsync(a =>
                a.BarberId == SelectedBarberId &&
                a.StartTime < endTime &&
                a.EndTime > startTime);

            if (!isStillFree)
            {
                TempData["ErrorMessage"] = "Този час вече е зает. Моля, изберете друг.";
                return RedirectToPage();
            }

            var appointment = new Appointment
            {
                ClientUserId = user.Id,
                BarberId = SelectedBarberId,
                ServiceId = SelectedServiceId,
                StartTime = startTime,
                EndTime = endTime,
                IsCompleted = false,
                CreatedAt = DateTime.Now
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Резервацията е успешна!";
            return RedirectToPage("/Index");
        }
    }
}
