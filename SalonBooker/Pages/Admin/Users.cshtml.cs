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
    public class UsersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UsersModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<UserInfoDto> Users { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;

        public class UserInfoDto
        {
            public string Id { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public int Points { get; set; }
            public bool IsActive { get; set; }
            public bool IsAdmin { get; set; }
            public bool IsBarber { get; set; }
        }

        public async Task OnGetAsync(string searchTerm = "", int currentPage = 1)
        {
            SearchTerm = searchTerm ?? "";
            CurrentPage = currentPage;

            var query = _context.Users.AsQueryable();

            // Търсене
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(u => u.FullName.Contains(SearchTerm) ||
                                         u.Email.Contains(SearchTerm) ||
                                         (u.PhoneNumber != null && u.PhoneNumber.Contains(SearchTerm)));
            }

            // Общ брой за пагинация
            var totalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

            // Вземане на потребителите за текущата страница
            var users = await query
                .OrderBy(u => u.FullName)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Зареждане на ролите за всеки потребител
            var barberUserIds = await _context.Barbers.Select(b => b.UserId).ToListAsync();
            var adminUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == _context.Roles.FirstOrDefault(r => r.Name == "Admin").Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            Users = users.Select(u => new UserInfoDto
            {
                Id = u.Id,
                FullName = u.FullName ?? u.UserName ?? "Неизвестен",
                Email = u.Email ?? string.Empty,
                PhoneNumber = u.PhoneNumber ?? string.Empty,
                Points = u.Points,
                IsActive = u.IsActive,
                IsAdmin = adminUserIds.Contains(u.Id),
                IsBarber = barberUserIds.Contains(u.Id)
            }).ToList();
        }

        public async Task<IActionResult> OnPostToggleActiveAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = $"Потребителят {(user.IsActive ? "е активиран" : "е блокиран")}.";
            }
            return RedirectToPage(new { searchTerm = SearchTerm, currentPage = CurrentPage });
        }

        public async Task<IActionResult> OnPostAddPointsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.Points += 10;
                if (!user.IsActive && user.Points > 0)
                    user.IsActive = true;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = $"Добавени 10 точки на {user.FullName}. Общо: {user.Points}";
            }
            return RedirectToPage(new { searchTerm = SearchTerm, currentPage = CurrentPage });
        }

        public async Task<IActionResult> OnPostRemovePointsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.Points = Math.Max(0, user.Points - 10);
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = $"Махнати 10 точки от {user.FullName}. Общо: {user.Points}";
            }
            return RedirectToPage(new { searchTerm = SearchTerm, currentPage = CurrentPage });
        }
    }


}