using HamroGrocery.Models.Entities;

namespace HamroGrocery.Models.ViewModels
{
    public class UserManagementViewModel
    {
        public List<UserCardDto> Users { get; set; } = new();
        public UserStatistics Statistics { get; set; } = new();
        public LoyaltySettings? LoyaltySettings { get; set; }
    }

    public class UserCardDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string Role { get; set; } = string.Empty;
        public string MembershipTier { get; set; } = string.Empty;
        public int LoyaltyPoints { get; set; }
        public int TotalPurchases { get; set; }
        public decimal TotalSpent { get; set; }
        public int ReviewCount { get; set; }
        public int ComplaintCount { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserStatistics
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsersThisMonth { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int PendingComplaints { get; set; }
        public int PendingReviews { get; set; }

        // Tier Distribution
        public int BronzeUsers { get; set; }
        public int SilverUsers { get; set; }
        public int GoldUsers { get; set; }
        public int PlatinumUsers { get; set; }
        public int DiamondUsers { get; set; }
    }
}