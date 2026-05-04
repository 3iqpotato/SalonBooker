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
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public int UsersCount { get; set; }
        public int ActiveUsersCount { get; set; }
        public int BlockedUsersCount { get; set; }
        public int BarbersCount { get; set; }
        public int ServicesCount { get; set; }
        public int AppointmentsCount { get; set; }
        public int ActiveAppointmentsCount { get; set; }
        public int CompletedAppointmentsCount { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            UsersCount = await _context.Users.CountAsync();
            ActiveUsersCount = await _context.Users.CountAsync(u => u.IsActive);
            BlockedUsersCount = UsersCount - ActiveUsersCount;
            BarbersCount = await _context.Barbers.CountAsync();
            ServicesCount = await _context.Services.CountAsync();
            AppointmentsCount = await _context.Appointments.CountAsync();
            ActiveAppointmentsCount = await _context.Appointments
                .CountAsync(a => !a.IsCompleted && a.StartTime > DateTime.Now);
            CompletedAppointmentsCount = await _context.Appointments
                .CountAsync(a => a.IsCompleted);
            return Page();
        }
    }
}