using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace SalonBooker.Models
{
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0, 500)]
        [Precision(18, 2)]
        public decimal Price { get; set; }

        [Required]
        [Range(15, 240)]
        public int DurationMinutes { get; set; } = 30;

        [Required]
        [Range(0, 50)]
        public int PointsAwarded { get; set; } = 5;

        // Навигационни свойства
        public virtual ICollection<BarberService> BarberServices { get; set; } = new List<BarberService>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}