using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalonBooker.Models
{
    public class BarberService
    {
        [Key]
        public int Id { get; set; }

        public int BarberId { get; set; }
        [ForeignKey("BarberId")]
        public virtual Barber Barber { get; set; }

        public int ServiceId { get; set; }
        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; }
    }
}