using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SalonBooker.Data;
using SalonBooker.Models;

namespace SalonBooker.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class BarberServicesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BarberServicesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int BarberId { get; set; }
        public string BarberName { get; set; } = string.Empty;
        public List<ServiceDto> CurrentServices { get; set; } = new();
        public List<ServiceDto> AvailableServices { get; set; } = new();

        public class ServiceDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int barberId)
        {
            var barber = await _context.Barbers
                .Include(b => b.User)
                .Include(b => b.BarberServices)
                    .ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(b => b.Id == barberId);

            if (barber == null)
                return NotFound();

            BarberId = barberId;
            BarberName = barber.User?.FullName ?? "Неизвестен";

            var currentServiceIds = barber.BarberServices.Select(bs => bs.ServiceId).ToList();

            CurrentServices = barber.BarberServices.Select(bs => new ServiceDto
            {
                Id = bs.Service.Id,
                Name = bs.Service.Name,
                Price = bs.Service.Price
            }).ToList();

            AvailableServices = await _context.Services
                .Where(s => !currentServiceIds.Contains(s.Id))
                .Select(s => new ServiceDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Price = s.Price
                })
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync(int barberId, int serviceId)
        {
            var alreadyExists = await _context.BarberServices
                .AnyAsync(bs => bs.BarberId == barberId && bs.ServiceId == serviceId);

            if (!alreadyExists)
            {
                _context.BarberServices.Add(new BarberService
                {
                    BarberId = barberId,
                    ServiceId = serviceId
                });
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Услугата е добавена.";
            }

            return RedirectToPage(new { barberId });
        }

        public async Task<IActionResult> OnPostRemoveAsync(int barberId, int serviceId)
        {
            var barberService = await _context.BarberServices
                .FirstOrDefaultAsync(bs => bs.BarberId == barberId && bs.ServiceId == serviceId);

            if (barberService != null)
            {
                _context.BarberServices.Remove(barberService);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Услугата е махната.";
            }

            return RedirectToPage(new { barberId });
        }
    }
}