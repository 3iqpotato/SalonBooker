using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SalonBooker.Data;
using SalonBooker.Models;
using SalonBooker.Services.Interfaces;

namespace SalonBooker.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<bool> IsBarberAsync(string userId)
            => await _context.Barbers.AnyAsync(b => b.UserId == userId);

        public async Task<string?> GetCannotBookReasonAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            // ПРЕДИ: директна заявка в UserRoles + Roles таблиците
            // var isAdmin = await _context.UserRoles.AnyAsync(ur =>
            //     ur.UserId == userId &&
            //     _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin"));

            // СЛЕД: готовият Identity метод
            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return null;

            if (!user.IsActive)
                return "Акаунтът ви е блокиран. Не можете да правите резервации.";

            if (user.Points <= 0)
                return "Нямате достатъчно точки за нова резервация.";

            var activeBookings = await _context.Appointments
                .CountAsync(a => a.ClientUserId == userId
                    && !a.IsCompleted
                    && a.StartTime > DateTime.Now);

            if (activeBookings >= 2)
                return $"Имате {activeBookings} активни резервации. Максималният брой е 2.";

            return null;
        }
    }
}