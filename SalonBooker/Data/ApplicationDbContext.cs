using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SalonBooker.Models;

namespace SalonBooker.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Barber> Barbers { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<BarberService> BarberServices { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Клиент изтрит → резервациите му се изтриват
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Client)
                .WithMany()
                .HasForeignKey(a => a.ClientUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Barber изтрит → резервациите НЕ се изтриват автоматично
            // (избягваме multiple cascade paths)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Barber)
                .WithMany(b => b.Appointments)
                .HasForeignKey(a => a.BarberId)
                .OnDelete(DeleteBehavior.NoAction);

            // Услуга изтрита → резервациите с нея се изтриват
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Service)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // BarberService - Barber страната NoAction
            modelBuilder.Entity<BarberService>()
                .HasOne(bs => bs.Barber)
                .WithMany(b => b.BarberServices)
                .HasForeignKey(bs => bs.BarberId)
                .OnDelete(DeleteBehavior.NoAction);

            // Услуга изтрита → BarberService записите се изтриват
            modelBuilder.Entity<BarberService>()
                .HasOne(bs => bs.Service)
                .WithMany(s => s.BarberServices)
                .HasForeignKey(bs => bs.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}