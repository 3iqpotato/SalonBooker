using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SalonBooker.Data;
using SalonBooker.Models;

namespace SalonBooker.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class BarberProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public BarberProfileModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public TimeOnly WorkStartTime { get; set; }
        public TimeOnly WorkEndTime { get; set; }
        public BarberDataDto BarberData { get; set; } = new();
        public string EditMode { get; set; } = "view";

        [BindProperty]
        public BarberInputModel Input { get; set; } = new();

        public class BarberDataDto
        {
            public string Bio { get; set; } = string.Empty;
            public string ProfileImageUrl { get; set; } = string.Empty;
        }

        public class BarberInputModel
        {
            [Display(Name = "Биография")]
            [StringLength(500, ErrorMessage = "Биографията може да е до {1} символа.")]
            public string Bio { get; set; } = string.Empty;

            [Display(Name = "URL на профилна снимка")]
            [Url(ErrorMessage = "Въведете валиден URL адрес")]
            public string ProfileImageUrl { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(string editMode = "view")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var barber = await _context.Barbers
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.UserId == user.Id);

            if (barber == null)
            {
                TempData["ErrorMessage"] = "Нямате права като фризьор.";
                return RedirectToPage("./Index");
            }

            EditMode = editMode;
            FullName = barber.User.FullName ?? barber.User.UserName ?? "Неизвестен";
            Email = barber.User.Email ?? string.Empty;
            WorkStartTime = barber.WorkStartTime;
            WorkEndTime = barber.WorkEndTime;
            BarberData.Bio = barber.Bio ?? string.Empty;
            BarberData.ProfileImageUrl = barber.ProfileImageUrl ?? string.Empty;

            if (editMode == "edit")
            {
                Input.Bio = BarberData.Bio;
                Input.ProfileImageUrl = BarberData.ProfileImageUrl;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var barber = await _context.Barbers
                .FirstOrDefaultAsync(b => b.UserId == user.Id);

            if (barber == null)
            {
                TempData["ErrorMessage"] = "Нямате права като фризьор.";
                return RedirectToPage("./Index");
            }

            if (!ModelState.IsValid)
            {
                FullName = barber.User.FullName ?? barber.User.UserName ?? "Неизвестен";
                Email = barber.User.Email ?? string.Empty;
                WorkStartTime = barber.WorkStartTime;
                WorkEndTime = barber.WorkEndTime;
                BarberData.Bio = barber.Bio ?? string.Empty;
                BarberData.ProfileImageUrl = barber.ProfileImageUrl ?? string.Empty;
                EditMode = "edit";
                return Page();
            }

            barber.Bio = Input.Bio ?? string.Empty;
            barber.ProfileImageUrl = Input.ProfileImageUrl ?? string.Empty;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Фризьорският профил беше обновен успешно!";
            return RedirectToPage(new { editMode = "view" });
        }
    }
}