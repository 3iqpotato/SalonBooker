using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SalonBooker.Data;
using SalonBooker.Models;

namespace SalonBooker.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Barber> Barbers { get; set; } = new();

        // Причина защо не може да резервира (null = може)
        public string? CannotBookReason { get; set; }

        public async Task OnGetAsync()
        {
            Barbers = await _context.Barbers
                .Include(b => b.User)
                .Include(b => b.BarberServices)
                    .ThenInclude(bs => bs.Service)
                .ToListAsync();

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                if (!user.IsActive)
                {
                    CannotBookReason = "Акаунтът ви е блокиран. Не можете да правите резервации.";
                }
                else if (user.Points <= 0)
                {
                    CannotBookReason = "Нямате достатъчно точки за нова резервация.";
                }
                else
                {
                    var activeBookings = await _context.Appointments
                        .CountAsync(a => a.ClientUserId == user.Id
                            && !a.IsCompleted
                            && a.StartTime > DateTime.Now);

                    if (activeBookings >= 2)
                    {
                        CannotBookReason = $"Имате {activeBookings} активни резервации. Максималният брой е 2.";
                    }
                }
            }
        }
    }
}