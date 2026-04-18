using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SalonBooker.Data;
using SalonBooker.Models;
using System.Security.Claims;

namespace SalonBooker.Pages.Appointments
{
    [Authorize]
    public class TodayModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TodayModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime SelectedDate { get; set; } = DateTime.Today;

        public Barber CurrentBarber { get; set; }
        public List<AppointmentDto> CurrentAppointments { get; set; } = new();
        public List<AppointmentDto> PastUnmarkedAppointments { get; set; } = new();
        public List<DateTime> AvailableDates { get; set; } = new();

        public class AppointmentDto
        {
            public int Id { get; set; }
            public string ClientName { get; set; } = string.Empty;
            public string ClientEmail { get; set; } = string.Empty;
            public string ServiceName { get; set; } = string.Empty;
            public decimal ServicePrice { get; set; }
            public int ServiceDuration { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public bool IsCompleted { get; set; }
            public bool IsPast { get; set; }
            public string StatusColor { get; set; } = string.Empty;
            public bool CanMark { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Проверка дали текущият потребител е фризьор
            CurrentBarber = await _context.Barbers
                .FirstOrDefaultAsync(b => b.UserId == user.Id);

            if (CurrentBarber == null)
            {
                TempData["ErrorMessage"] = "Нямате права като фризьор.";
                return RedirectToPage("/Index");
            }

            // Генерираме налични дати (днес + следващите 14 дни)
            for (int i = 0; i < 14; i++)
            {
                var date = DateTime.Today.AddDays(i);
                if (date.DayOfWeek != DayOfWeek.Sunday) // без неделя
                {
                    AvailableDates.Add(date);
                }
            }

            await LoadAppointmentsForDate(SelectedDate);

            return Page();
        }

        public async Task<IActionResult> OnGetLoadAppointmentsAsync(DateTime date)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return new JsonResult(new { error = "Not authenticated" }) { StatusCode = 401 };
            }

            var barber = await _context.Barbers.FirstOrDefaultAsync(b => b.UserId == user.Id);
            if (barber == null)
            {
                return new JsonResult(new { error = "Not a barber" }) { StatusCode = 403 };
            }

            var appointments = await GetAppointmentsForDate(barber.Id, date);
            return new JsonResult(appointments);
        }

        private async Task LoadAppointmentsForDate(DateTime date)
        {
            CurrentAppointments = await GetAppointmentsForDate(CurrentBarber.Id, date);

            // Зареждаме минали неизпълнени резервации (преди днес)
            PastUnmarkedAppointments = await _context.Appointments
                .Where(a => a.BarberId == CurrentBarber.Id
                    && !a.IsCompleted
                    && a.StartTime.Date < DateTime.Today)
                .Include(a => a.Client)
                .Include(a => a.Service)
                .OrderByDescending(a => a.StartTime)
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    ClientName = a.Client.FullName ?? a.Client.UserName ?? "Неизвестен",
                    ClientEmail = a.Client.Email ?? "",
                    ServiceName = a.Service.Name,
                    ServicePrice = a.Service.Price,
                    ServiceDuration = a.Service.DurationMinutes,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    IsCompleted = a.IsCompleted,
                    IsPast = true,
                    StatusColor = "warning",
                    CanMark = !a.IsCompleted
                })
                .ToListAsync();
        }

        private async Task<List<AppointmentDto>> GetAppointmentsForDate(int barberId, DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = date.Date.AddDays(1);
            var now = DateTime.Now;

            var appointments = await _context.Appointments
                .Where(a => a.BarberId == barberId
                    && a.StartTime >= startOfDay
                    && a.StartTime < endOfDay)
                .Include(a => a.Client)
                .Include(a => a.Service)
                .OrderBy(a => a.StartTime)
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    ClientName = a.Client.FullName ?? a.Client.UserName ?? "Неизвестен",
                    ClientEmail = a.Client.Email ?? "",
                    ServiceName = a.Service.Name,
                    ServicePrice = a.Service.Price,
                    ServiceDuration = a.Service.DurationMinutes,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    IsCompleted = a.IsCompleted,
                    IsPast = a.StartTime < now,
                    CanMark = !a.IsCompleted && a.StartTime < now
                })
                .ToListAsync();

            // Определяне на цветове за всяка резервация
            foreach (var app in appointments)
            {
                if (app.IsCompleted)
                {
                    app.StatusColor = "success"; // Зелено - изпълнена
                }
                else if (app.StartTime < now)
                {
                    app.StatusColor = "danger"; // Червено - минала, но неизпълнена
                    app.CanMark = true; // Може да се маркира
                }
                else
                {
                    // Бъдещи резервации
                    var timeUntilStart = (app.StartTime - now).TotalMinutes;
                    if (timeUntilStart <= 30 && timeUntilStart > 0)
                    {
                        app.StatusColor = "warning"; // Оранжево - започва скоро
                    }
                    else
                    {
                        app.StatusColor = "info"; // Синьо - нормална бъдеща
                    }
                    app.CanMark = false; // Бъдещите не могат да се маркират
                }
            }

            return appointments;
        }

        public async Task<IActionResult> OnPostMarkAsCompletedAsync(int appointmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var barber = await _context.Barbers.FirstOrDefaultAsync(b => b.UserId == user.Id);
            if (barber == null)
            {
                TempData["ErrorMessage"] = "Нямате права като фризьор.";
                return RedirectToPage("/Index");
            }

            var appointment = await _context.Appointments
                .Include(a => a.Client)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.BarberId == barber.Id);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Резервацията не е намерена.";
                return RedirectToPage();
            }

            if (appointment.IsCompleted)
            {
                TempData["ErrorMessage"] = "Тази резервация вече е маркирана като изпълнена.";
                return RedirectToPage();
            }

            // Маркиране като изпълнена
            appointment.IsCompleted = true;

            // Добавяне на точки на клиента
            var client = appointment.Client;
            if (client != null)
            {
                client.Points += 5;
                await _userManager.UpdateAsync(client);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Резервацията за {appointment.StartTime:HH:mm} е маркирана като изпълнена. Клиентът получи +5 точки.";

            return RedirectToPage(new { SelectedDate = appointment.StartTime.Date });
        }

        public async Task<IActionResult> OnPostMarkAsNoShowAsync(int appointmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var barber = await _context.Barbers.FirstOrDefaultAsync(b => b.UserId == user.Id);
            if (barber == null)
            {
                TempData["ErrorMessage"] = "Нямате права като фризьор.";
                return RedirectToPage("/Index");
            }

            var appointment = await _context.Appointments
                .Include(a => a.Client)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.BarberId == barber.Id);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Резервацията не е намерена.";
                return RedirectToPage();
            }

            if (appointment.IsCompleted)
            {
                TempData["ErrorMessage"] = "Тази резервация вече е маркирана.";
                return RedirectToPage();
            }

            // Изтриваме резервацията (като неявяване)
            var client = appointment.Client;
            var appointmentTime = appointment.StartTime;

            _context.Appointments.Remove(appointment);

            // Намаляване на точки на клиента
            if (client != null)
            {
                client.Points -= 5;
                if (client.Points < 0) client.Points = 0;
                await _userManager.UpdateAsync(client);
            }

            await _context.SaveChangesAsync();

            TempData["WarningMessage"] = $"Резервацията за {appointmentTime:HH:mm} е маркирана като неявяване. Клиентът загуби 5 точки.";

            return RedirectToPage(new { SelectedDate = appointmentTime.Date });
        }
    }
}