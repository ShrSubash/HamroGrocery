using HamroGrocery.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HamroGrocery.Data
{
    /// <summary>
    /// Database seeder for initial data including Guest User
    /// </summary>
    public static class DbSeeder
    {
        /// <summary>
        /// Seeds essential data including the Guest User account
        /// </summary>
        public static async Task SeedEssentialDataAsync(
            ApplicationDbContext context,
            UserManager<User> userManager,
            ILogger logger)
        {
            try
            {
                logger.LogInformation("Starting database seeding...");

                // Seed Guest User (Required for guest checkout)
                await SeedGuestUserAsync(context, userManager, logger);

                await context.SaveChangesAsync();
                logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during database seeding");
                throw;
            }
        }

        /// <summary>
        /// Creates or updates the system Guest User account
        /// </summary>
        private static async Task SeedGuestUserAsync(
            ApplicationDbContext context,
            UserManager<User> userManager,
            ILogger logger)
        {
            const string GUEST_EMAIL = "guest@hamrogrocery.system";
            const string GUEST_USERNAME = "guest_system";

            try
            {
                // Check if guest user already exists
                var existingGuest = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == 1 || u.Email == GUEST_EMAIL);

                if (existingGuest != null)
                {
                    logger.LogInformation("Guest User already exists with ID: {Id}", existingGuest.Id);
                    return;
                }

                // Create new Guest User
                var guestUser = new User
                {
                    Id = 1, // Fixed ID for guest user
                    UserName = GUEST_USERNAME,
                    NormalizedUserName = GUEST_USERNAME.ToUpper(),
                    Email = GUEST_EMAIL,
                    NormalizedEmail = GUEST_EMAIL.ToUpper(),
                    EmailConfirmed = true,
                    FirstName = "Guest",
                    LastName = "User",
                    PhoneNumber = "0400000000",
                    LockoutEnabled = false,
                    AccessFailedCount = 0
                };

                // Set a secure password (not used, but required by Identity)
                var passwordHash = userManager.PasswordHasher.HashPassword(
                    guestUser,
                    Guid.NewGuid().ToString("N") + "!Aa1" // Random secure password
                );
                guestUser.PasswordHash = passwordHash;

                // Add to database with explicit ID
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Users ON");
                context.Users.Add(guestUser);
                await context.SaveChangesAsync();
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Users OFF");

                logger.LogInformation("Created Guest User account with ID: 1");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding Guest User");
                throw;
            }
        }

        /// <summary>
        /// Ensures Guest User exists (called before each guest checkout)
        /// </summary>
        public static async Task<bool> EnsureGuestUserExistsAsync(
            ApplicationDbContext context,
            ILogger logger)
        {
            try
            {
                var guestUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == 1);

                if (guestUser == null)
                {
                    logger.LogWarning("Guest User not found - database may not be seeded properly");

                    // Attempt to create guest user on the fly
                    try
                    {
                        var newGuestUser = new User
                        {
                            Id = 1,
                            UserName = "guest_system",
                            NormalizedUserName = "GUEST_SYSTEM",
                            Email = "guest@hamrogrocery.system",
                            NormalizedEmail = "GUEST@HAMROGROCERY.SYSTEM",
                            EmailConfirmed = true,
                            FirstName = "Guest",
                            LastName = "User",
                            PhoneNumber = "0400000000",
                            SecurityStamp = Guid.NewGuid().ToString(),
                            PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForGuestUserNotUsedForLogin=="
                        };

                        await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Users ON");
                        context.Users.Add(newGuestUser);
                        await context.SaveChangesAsync();
                        await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Users OFF");

                        logger.LogInformation("Guest User created on-the-fly with ID: 1");
                        return true;
                    }
                    catch (Exception createEx)
                    {
                        logger.LogError(createEx, "Failed to create Guest User on-the-fly");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking Guest User existence");
                return false;
            }
        }
    }
}