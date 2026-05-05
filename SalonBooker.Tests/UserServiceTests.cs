using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using SalonBooker.Data;
using SalonBooker.Models;
using SalonBooker.Services;

namespace SalonBooker.Tests
{
    public class UserServiceTests
    {
        // Помощен метод — нова изолирана InMemory база за всеки тест
        private ApplicationDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        // Помощен метод — фалшив UserManager, никой не е Admin
        private UserManager<ApplicationUser> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mgr = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            mgr.Setup(m => m.IsInRoleAsync(
                    It.IsAny<ApplicationUser>(), "Admin"))
                .ReturnsAsync(false);

            return mgr.Object;
        }

        [Fact]
        public async Task GetCannotBookReason_BlockedUser_ReturnsBlockedMessage()
        {
            var db = CreateInMemoryDb();
            var userManager = CreateMockUserManager(); // ново
            var user = new ApplicationUser
            {
                Id = "user1",
                UserName = "test@test.com",
                Email = "test@test.com",
                FullName = "Тест Потребител",
                IsActive = false, // блокиран
                Points = 30
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var service = new UserService(db, userManager); // ново

            var result = await service.GetCannotBookReasonAsync("user1");

            Assert.NotNull(result);
            Assert.Contains("блокиран", result);
        }

        [Fact]
        public async Task GetCannotBookReason_ZeroPoints_ReturnsPointsMessage()
        {
            var db = CreateInMemoryDb();
            var userManager = CreateMockUserManager();
            var user = new ApplicationUser
            {
                Id = "user2",
                UserName = "test2@test.com",
                Email = "test2@test.com",
                FullName = "Тест Потребител 2",
                IsActive = true,
                Points = 0 // нула точки
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var service = new UserService(db, userManager);

            var result = await service.GetCannotBookReasonAsync("user2");

            Assert.NotNull(result);
            Assert.Contains("точки", result);
        }

        [Fact]
        public async Task GetCannotBookReason_TwoActiveBookings_ReturnsMaxBookingsMessage()
        {
            var db = CreateInMemoryDb();
            var userManager = CreateMockUserManager();
            var user = new ApplicationUser
            {
                Id = "user3",
                UserName = "test3@test.com",
                Email = "test3@test.com",
                FullName = "Тест Потребител 3",
                IsActive = true,
                Points = 30
            };
            db.Users.Add(user);
            db.Appointments.AddRange(
                new Appointment
                {
                    ClientUserId = "user3",
                    BarberId = 1,
                    ServiceId = 1,
                    StartTime = DateTime.Now.AddDays(1),
                    EndTime = DateTime.Now.AddDays(1).AddMinutes(30),
                    IsCompleted = false
                },
                new Appointment
                {
                    ClientUserId = "user3",
                    BarberId = 1,
                    ServiceId = 1,
                    StartTime = DateTime.Now.AddDays(2),
                    EndTime = DateTime.Now.AddDays(2).AddMinutes(30),
                    IsCompleted = false
                }
            );
            await db.SaveChangesAsync();

            var service = new UserService(db, userManager);

            var result = await service.GetCannotBookReasonAsync("user3");

            Assert.NotNull(result);
            Assert.Contains("резервации", result);
        }

        [Fact]
        public async Task GetCannotBookReason_ActiveUserWithPoints_ReturnsNull()
        {
            var db = CreateInMemoryDb();
            var userManager = CreateMockUserManager();
            var user = new ApplicationUser
            {
                Id = "user4",
                UserName = "test4@test.com",
                Email = "test4@test.com",
                FullName = "Тест Потребител 4",
                IsActive = true,
                Points = 30 // нормален потребител
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var service = new UserService(db, userManager);

            var result = await service.GetCannotBookReasonAsync("user4");

            Assert.Null(result); // няма причина да блокира
        }

        [Fact]
        public async Task IsBarberAsync_ExistingBarber_ReturnsTrue()
        {
            var db = CreateInMemoryDb();
            var userManager = CreateMockUserManager();
            var user = new ApplicationUser
            {
                Id = "user5",
                UserName = "barber@test.com",
                Email = "barber@test.com",
                FullName = "Фризьор Тест"
            };
            db.Users.Add(user);
            db.Barbers.Add(new Barber
            {
                UserId = "user5",
                WorkStartTime = new TimeOnly(9, 0),
                WorkEndTime = new TimeOnly(18, 0)
            });
            await db.SaveChangesAsync();

            var service = new UserService(db, userManager);

            var result = await service.IsBarberAsync("user5");

            Assert.True(result);
        }

        [Fact]
        public async Task IsBarberAsync_NotABarber_ReturnsFalse()
        {
            var db = CreateInMemoryDb();
            var userManager = CreateMockUserManager();
            var service = new UserService(db, userManager);

            var result = await service.IsBarberAsync("nonexistent");

            Assert.False(result);
        }
    }
}