using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SalonBooker.Data;
using SalonBooker.Models;
using SalonBooker.Services.Interfaces;

namespace SalonBooker.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserService _userService;

        public IndexModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IUserService userService)
        {
            _context = context;
            _userManager = userManager;
            _userService = userService;
        }

        public List<Barber> Barbers { get; set; } = new();
        public string? CannotBookReason { get; set; }

        public async Task OnGetAsync()
        {
            Barbers = await _context.Barbers
                .Include(b => b.User)
                .Include(b => b.BarberServices)
                    .ThenInclude(bs => bs.Service)
                    .Where(b => b.User.IsActive)
                .ToListAsync();

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
                CannotBookReason = await _userService.GetCannotBookReasonAsync(user.Id);
        }
    }
}