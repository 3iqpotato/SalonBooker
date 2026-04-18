using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SalonBooker.Data;
using SalonBooker.Models;

namespace SalonBooker.Pages.Appointments
{
    [Authorize]
    public class MyAppointmentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MyAppointmentsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<UserAppointmentDto> UpcomingAppointments { get; set; } = new();
        public List<UserAppointmentDto> PastAppointments { get; set; } = new();

        public class UserAppointmentDto
        {
            public int Id { get; set; }
            public string BarberName { get; set; } = string.Empty;
            public string ServiceName { get; set; } = string.Empty;
            public decimal ServicePrice { get; set; }
            public int ServiceDuration { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public bool IsCompleted { get; set; }
            public bool CanCancel { get; set; }
            public int PointsToLose { get; set; }
        }

        // СТАТИЧЕН МЕТОД - вече може да се използва
        private static int CalculatePointsToLose(DateTime appointmentStart, DateTime now)
        {
            var hoursUntilAppointment = (appointmentStart - now).TotalHours;

            if (hoursUntilAppointment > 48) // Повече от 2 дни
            {
                return 1;
            }
            else // Под 2 дни или същия ден
            {
                return 5;
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var now = DateTime.Now;

            // Първо зареждаме данните от базата (без да изчисляваме PointsToLose)
            var appointmentsRaw = await _context.Appointments
                .Where(a => a.ClientUserId == user.Id)
                .Include(a => a.Barber)
                    .ThenInclude(b => b.User)
                .Include(a => a.Service)
                .Select(a => new UserAppointmentDto
                {
                    Id = a.Id,
                    BarberName = a.Barber.User.FullName ?? a.Barber.User.UserName ?? "Неизвестен",
                    ServiceName = a.Service.Name,
                    ServicePrice = a.Service.Price,
                    ServiceDuration = a.Service.DurationMinutes,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    IsCompleted = a.IsCompleted,
                    CanCancel = !a.IsCompleted && a.StartTime > now
                    // PointsToLose не изчисляваме тук
                })
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();

            // След това, в паметта, изчисляваме PointsToLose за всяка резервация
            foreach (var appointment in appointmentsRaw)
            {
                appointment.PointsToLose = CalculatePointsToLose(appointment.StartTime, now);
            }

            // Разделяме на бъдещи и минали
            UpcomingAppointments = appointmentsRaw.Where(a => a.StartTime > now && !a.IsCompleted).ToList();
            PastAppointments = appointmentsRaw.Where(a => a.StartTime <= now || a.IsCompleted).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostCancelAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Намери резервацията
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.ClientUserId == user.Id);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Резервацията не е намерена.";
                return RedirectToPage();
            }

            // Проверка дали може да се откаже
            if (appointment.IsCompleted)
            {
                TempData["ErrorMessage"] = "Не можете да откажете вече изпълнена резервация.";
                return RedirectToPage();
            }

            if (appointment.StartTime <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Не можете да откажете резервация, която вече е минала.";
                return RedirectToPage();
            }

            // Изчисляваме точките за отказване
            var pointsToLose = CalculatePointsToLose(appointment.StartTime, DateTime.Now);

            // Намаляваме точките на потребителя
            user.Points -= pointsToLose;

            // Ако точките станат 0 или по-малко, блокираме потребителя
            if (user.Points <= 0)
            {
                user.Points = 0;
                user.IsActive = false;
                TempData["WarningMessage"] = $"Загубихте {pointsToLose} точки. Точките Ви са 0. Не можете да правите нови резервации, докато не получите точки от фризьор.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Успешно отказахте резервацията. Загубихте {pointsToLose} точки. Оставащи точки: {user.Points}";
            }

            await _userManager.UpdateAsync(user);

            // Изтриваме резервацията
            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}