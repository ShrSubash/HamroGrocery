// File: Services/DatabaseInitializer.cs
using HamroGrocery.Data;
using HamroGrocery.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HamroGrocery.Services
{
    public static class DatabaseInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext db)
        {
            // Only seed LoyaltySettings if it doesn't exist
            if (!await db.LoyaltySettings.AnyAsync())
            {
                db.LoyaltySettings.Add(new LoyaltySettings
                {
                    Id = 1,
                    // All defaults come from your class — no need to write them again!
                    // CreatedAt = DateTime.UtcNow → will be set automatically from property initializer
                });

                await db.SaveChangesAsync();
            }
        }
    }
}