using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalonBooker.Models
{
    public class Barber
    {
        [Key]
        public int Id { get; set; }

        // Връзка към потребителя (IdentityUser)
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // Допълнителни данни за фризьора
        public string Bio { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;

        // Работно време
        public TimeOnly WorkStartTime { get; set; } = new TimeOnly(9, 0);  // 09:00
        public TimeOnly WorkEndTime { get; set; } = new TimeOnly(18, 0);   // 18:00
        public int SlotDurationMinutes { get; set; } = 30;  // 30 минути

        // Навигационни свойства
        public virtual ICollection<BarberService> BarberServices { get; set; } = new List<BarberService>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}