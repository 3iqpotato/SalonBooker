//using Microsoft.AspNetCore.Identity;
//using SalonBooker.Models;

//namespace SalonBooker.Data
//{
//    public static class SeedData
//    {
//        public static async Task InitializeAsync(IServiceProvider serviceProvider)
//        {
//            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
//            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

//            // 1. Създаване на роля Admin (ако не съществува)
//            if (!await roleManager.RoleExistsAsync("Admin"))
//            {
//                await roleManager.CreateAsync(new IdentityRole("Admin"));
//                Console.WriteLine("✅ Роля 'Admin' създадена.");
//            }
//            else
//            {
//                Console.WriteLine("ℹ️ Роля 'Admin' вече съществува.");
//            }

//            // 2. Намиране на администраторския потребител
//            var adminUser = await userManager.FindByEmailAsync("admin@salon.com");

//            if (adminUser != null)
//            {
//                // 3. Добавяне на потребителя към роля Admin (ако не е вече)
//                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
//                {
//                    await userManager.AddToRoleAsync(adminUser, "Admin");
//                    Console.WriteLine("✅ Потребителят admin@salon.com получи роля 'Admin'.");
//                }
//                else
//                {
//                    Console.WriteLine("ℹ️ Потребителят admin@salon.com вече е администратор.");
//                }
//            }
//            else
//            {
//                Console.WriteLine("❌ Потребителят admin@salon.com не е намерен! Първо се регистрирайте.");
//            }
//        }
//    }
//}