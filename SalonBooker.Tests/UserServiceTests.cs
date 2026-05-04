using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using SalonBooker.Data;
using SalonBooker.Models;
using SalonBooker.Services;

namespace SalonBooker.Tests
{
    public class UserServiceTests
    {
        private ApplicationDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetCannotBookReason_BlockedUser_ReturnsBlockedMessage()
        {
            // Arrange
            var db = CreateInMemoryDb();
            var user = new ApplicationUser
            {
                Id = "user1",
                UserName = "test@test.com",
                Email = "test@test.com",
                FullName = "Тест Потребител",
                IsActive = false,
                Points = 30
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var service = new UserService(db);

            // Act
            var result = await service.GetCannotBookReasonAsync("user1");

            // Assert
            Assert.NotNull(result);
            Assert.Contains("блокиран", result);
        }

        [Fact]
        public async Task GetCannotBookReason_ZeroPoints_ReturnsPointsMessage()
        {
            // Arrange
            var db = CreateInMemoryDb();
            var user = new ApplicationUser
            {
                Id = "user2",
                UserName = "test2@test.com",
                Email = "test2@test.com",
                FullName = "Тест Потребител 2",
                IsActive = true,
                Points = 0
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var service = new UserService(db);

            // Act
            var result = await service.GetCannotBookReasonAsync("user2");

            // Assert
            Assert.NotNull(result);
            Assert.Contains("точки", result);
        }

        [Fact]
        public async Task GetCannotBookReason_TwoActiveBookings_ReturnsMaxBookingsMessage()
        {
            // Arrange
            var db = CreateInMemoryDb();
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

            var service = new UserService(db);

            // Act
            var result = await service.GetCannotBookReasonAsync("user3");

            // Assert
            Assert.NotNull(result);
            Assert.Contains("резервации", result);
        }

        [Fact]
        public async Task GetCannotBookReason_ActiveUserWithPoints_ReturnsNull()
        {
            // Arrange
            var db = CreateInMemoryDb();
            var user = new ApplicationUser
            {
                Id = "user4",
                UserName = "test4@test.com",
                Email = "test4@test.com",
                FullName = "Тест Потребител 4",
                IsActive = true,
                Points = 30
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var service = new UserService(db);

            // Act
            var result = await service.GetCannotBookReasonAsync("user4");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task IsBarberAsync_ExistingBarber_ReturnsTrue()
        {
            // Arrange
            var db = CreateInMemoryDb();
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

            var service = new UserService(db);

            // Act
            var result = await service.IsBarberAsync("user5");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsBarberAsync_NotABarber_ReturnsFalse()
        {
            // Arrange
            var db = CreateInMemoryDb();
            var service = new UserService(db);

            // Act
            var result = await service.IsBarberAsync("nonexistent");

            // Assert
            Assert.False(result);
        }
    }
}