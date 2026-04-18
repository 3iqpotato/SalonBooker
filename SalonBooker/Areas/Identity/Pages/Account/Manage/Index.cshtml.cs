using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalonBooker.Data;
using SalonBooker.Models;

namespace SalonBooker.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            ILogger<IndexModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        public string Email { get; set; } = string.Empty;
        public int Points { get; set; }
        public bool IsActive { get; set; }
        public DateTime RegisteredAt { get; set; }

        public string EditMode { get; set; } = "none";

        [BindProperty]
        public UserInputModel Input { get; set; } = new();

        public class UserInputModel
        {
            [Display(Name = "Пълно име")]
            [Required(ErrorMessage = "Пълното име е задължително")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Името трябва да е между {2} и {1} символа.")]
            public string FullName { get; set; } = string.Empty;

            [Display(Name = "Кратка биография")]
            [StringLength(500, ErrorMessage = "Биографията може да е до {1} символа.")]
            public string? Bio { get; set; }

            [Display(Name = "URL на профилна снимка")]
            [Url(ErrorMessage = "Въведете валиден URL адрес")]
            public string? ProfilePictureUrl { get; set; }

            [Display(Name = "Телефон")]
            [Phone(ErrorMessage = "Въведете валиден телефонен номер")]
            public string? PhoneNumber { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string editMode = "none")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("[PROFILE GET] Потребителят не е намерен.");
                return NotFound();
            }

            _logger.LogInformation("[PROFILE GET] Зареждане на профил за {Email}, editMode={EditMode}", user.Email, editMode);

            EditMode = editMode;
            await LoadAsync(user);
            return Page();
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            Email = user.Email ?? string.Empty;
            Points = user.Points;
            IsActive = user.IsActive;
            RegisteredAt = user.RegisteredAt;

            Input.FullName = user.FullName ?? string.Empty;
            Input.Bio = user.Bio;
            Input.ProfilePictureUrl = user.ProfilePictureUrl;
            Input.PhoneNumber = user.PhoneNumber;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("[USER SAVE] Получена POST заявка за потребителски профил.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("[USER SAVE] Потребителят не е намерен!");
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("[USER SAVE] ModelState невалиден: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                await LoadAsync(user);
                EditMode = "user";
                return Page();
            }

            _logger.LogInformation("[USER SAVE] Запазване: FullName={FullName}, Bio={Bio}", Input.FullName, Input.Bio);

            user.FullName = Input.FullName;
            user.Bio = Input.Bio ?? string.Empty;
            user.ProfilePictureUrl = Input.ProfilePictureUrl ?? string.Empty;
            user.PhoneNumber = Input.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    _logger.LogError("[USER SAVE] Грешка: {Error}", error.Description);
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await LoadAsync(user);
                EditMode = "user";
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("[USER SAVE] Успешно запазен потребителски профил.");
            TempData["SuccessMessage"] = "Потребителският профил беше обновен успешно!";
            return RedirectToPage(new { editMode = "none" });
        }
    }
}