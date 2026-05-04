using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SalonBooker.Data;
using SalonBooker.Models;

namespace SalonBooker.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class BarbersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BarbersModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<BarberDto> Barbers { get; set; } = new();

        public class BarberDto
        {
            public int BarberId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string WorkStart { get; set; } = string.Empty;
            public string WorkEnd { get; set; } = string.Empty;
            public bool IsActive { get; set; }
        }

        public async Task OnGetAsync()
        {
            Barbers = await _context.Barbers
                .Include(b => b.User)
                .Select(b => new BarberDto
                {
                    BarberId = b.Id,
                    FullName = b.User.FullName ?? b.User.UserName ?? "Неизвестен",
                    Email = b.User.Email ?? string.Empty,
                    WorkStart = b.WorkStartTime.ToString(@"hh\:mm"),
                    WorkEnd = b.WorkEndTime.ToString(@"hh\:mm"),
                    IsActive = b.User.IsActive
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAddBarberAsync(
            string email, string workStart, string workEnd)
        {
            // Намираме потребителя по имейл
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Не е намерен потребител с този имейл.";
                return RedirectToPage();
            }

            // Проверка дали вече е фризьор
            var alreadyBarber = await _context.Barbers.AnyAsync(b => b.UserId == user.Id);
            if (alreadyBarber)
            {
                TempData["ErrorMessage"] = "Този потребител вече е фризьор.";
                return RedirectToPage();
            }

            // Парсване на работното време
            if (!TimeOnly.TryParse(workStart, out var start) ||
                !TimeOnly.TryParse(workEnd, out var end))
            {
                TempData["ErrorMessage"] = "Невалидно работно време.";
                return RedirectToPage();
            }

            // Създаваме Barber запис
            var barber = new Barber
            {
                UserId = user.Id,
                WorkStartTime = start,
                WorkEndTime = end,
                SlotDurationMinutes = 30
            };

            _context.Barbers.Add(barber);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{user.FullName} е добавен като фризьор.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleBarberAsync(int barberId)
        {
            var barber = await _context.Barbers
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == barberId);

            if (barber == null)
            {
                TempData["ErrorMessage"] = "Фризьорът не е намерен.";
                return RedirectToPage();
            }

            // Блокираме/активираме потребителя
            barber.User.IsActive = !barber.User.IsActive;
            await _userManager.UpdateAsync(barber.User);

            TempData["SuccessMessage"] = $"{barber.User.FullName} е {(barber.User.IsActive ? "активиран" : "блокиран")}.";
            return RedirectToPage();
        }
    }
}