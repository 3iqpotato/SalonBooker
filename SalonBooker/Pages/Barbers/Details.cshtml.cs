using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SalonBooker.Data;
using SalonBooker.Models;

namespace SalonBooker.Pages.Barbers
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Barber Barber { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Barber = await _context.Barbers
                .Include(b => b.User)
                .Include(b => b.BarberServices)
                    .ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (Barber == null)
                return NotFound();

            return Page();
        }
    }
}