using Microsoft.AspNetCore.Identity;

namespace SalonBooker.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Пълно име на потребителя
        public string FullName { get; set; } = string.Empty;

        // Кратка биография (за фризьорите)
        public string Bio { get; set; } = string.Empty;

        // URL към профилна снимка (от интернет)
        public string ProfilePictureUrl { get; set; } = string.Empty;

        // Точки (започва с 10)
        public int Points { get; set; } = 30;

        // Дали потребителят е активен (може да резервира)
        public bool IsActive { get; set; } = true;

        // Дата на регистрация
        public DateTime RegisteredAt { get; set; } = DateTime.Now;
    }
}