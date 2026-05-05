using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SalonBooker.Data;
using SalonBooker.Models;
using SalonBooker.Services.Interfaces;

namespace SalonBooker.Pages.Barbers
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserService _userService;

        public DetailsModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IUserService userService)
        {
            _context = context;
            _userManager = userManager;
            _userService = userService;
        }

        public Barber Barber { get; set; }
        public bool CanBook { get; set; } // <-- ново

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Barber = await _context.Barbers
                .Include(b => b.User)
                .Include(b => b.BarberServices)
                    .ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (Barber == null)
                return NotFound();

            // Изчисляваме дали текущият потребител може да резервира
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                var isBarber = await _userService.IsBarberAsync(currentUser.Id);
                CanBook = !isBarber; // фризьори не могат
            }

            return Page();
        }
    }
}