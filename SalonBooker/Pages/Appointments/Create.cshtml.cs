using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SalonBooker.Data;
using SalonBooker.Models;
using SalonBooker.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace SalonBooker.Pages.Appointments
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAppointmentService _appointmentService;
        private readonly IUserService _userService;

        public CreateModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAppointmentService appointmentService,
            IUserService userService)
        {
            _context = context;
            _userManager = userManager;
            _appointmentService = appointmentService;
            _userService = userService;
        }

        [BindProperty] public int SelectedBarberId { get; set; }
        [BindProperty] public int SelectedServiceId { get; set; }
        [BindProperty] public string SelectedDate { get; set; } = string.Empty;
        [BindProperty] public string SelectedTime { get; set; } = string.Empty;

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
            var slots = await _appointmentService.GetAvailableSlotsAsync(
                request.BarberId, request.ServiceId, request.Date);

            return new JsonResult(slots);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login");

            if (await _userService.IsBarberAsync(user.Id))
            {
                TempData["ErrorMessage"] = "Фризьорите не могат да правят резервации.";
                return RedirectToPage("/Index");
            }

            // Проверки за потребителя през service
            var cannotBookReason = await _userService.GetCannotBookReasonAsync(user.Id);
            if (cannotBookReason != null)
            {
                TempData["ErrorMessage"] = cannotBookReason;
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

            // Race condition проверка през service
            if (!await _appointmentService.IsSlotFreeAsync(SelectedBarberId, startTime, endTime))
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