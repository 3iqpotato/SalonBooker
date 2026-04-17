using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalonBooker.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        // Клиентът (който резервира)
        public string ClientUserId { get; set; }
        [ForeignKey("ClientUserId")]
        public virtual ApplicationUser Client { get; set; }

        // Фризьорът
        public int BarberId { get; set; }
        [ForeignKey("BarberId")]
        public virtual Barber Barber { get; set; }

        // Услугата
        public int ServiceId { get; set; }
        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; }

        // Час на резервацията
        [Required]
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        // Дали е изпълнена (маркирано от фризьора)
        public bool IsCompleted { get; set; } = false;

        // Кога е създадена резервацията
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}