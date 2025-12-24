using Microsoft.AspNetCore.Identity;
using Stripe;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamroGrocery.Models.Entities
{
    public class User : IdentityUser<int>
    {
        // Existing Basic Info
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Existing Address Information
        public string? AddressLineOne { get; set; }
        public string? AddressLineTwo { get; set; }
        public string? PostalCode { get; set; }

        // Existing Foreign Keys
        public int? CountryId { get; set; }
        public int? StateProvinceId { get; set; }
        public int? CityId { get; set; }

        // Existing Role & Approval
        public UserRole Role { get; set; } = UserRole.Customer;
        public bool IsApproved { get; set; } = false;

        // NEW: Loyalty & Gamification Properties
        public int LoyaltyPoints { get; set; } = 0;
        public int TotalPurchases { get; set; } = 0;
        public decimal TotalSpent { get; set; } = 0;
        public MembershipTier MembershipTier { get; set; } = MembershipTier.Bronze;
        public DateTime? LastLoginAt { get; set; }

        // NEW: Statistics (calculated fields - updated via triggers/services)
        public int ReviewCount { get; set; } = 0;
        public decimal AverageRating { get; set; } = 0;
        public int ComplaintCount { get; set; } = 0;

        // NEW: Preferences
        public bool EnableEmailNotifications { get; set; } = true;
        public bool EnableSmsNotifications { get; set; } = false;

        // Existing Navigation Properties
        public Country? Country { get; set; }
        public StateProvince? StateProvince { get; set; }
        public City? City { get; set; }
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        // NEW: Navigation Properties for Reviews & Complaints
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();

        // Full Name Property
        [NotMapped]
        public string FullName => $"{FirstName} {MiddleName} {LastName}".Replace("  ", " ").Trim();
    }

    public enum UserRole
    {
        Customer = 1,
        Vendor = 2,
        Manager = 3,
        Admin = 4,
        SuperAdmin = 5
    }

    // NEW: Membership Tiers
    public enum MembershipTier
    {
        Bronze = 1,   // 0-9 purchases
        Silver = 2,   // 10-29 purchases
        Gold = 3,     // 30-49 purchases
        Platinum = 4, // 50-99 purchases
        Diamond = 5   // 100+ purchases
    }
}