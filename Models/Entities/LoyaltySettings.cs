using System.ComponentModel.DataAnnotations;

namespace HamroGrocery.Models.Entities
{
    public class LoyaltySettings
    {
        public int Id { get; set; }

        // Feature Toggles
        public bool IsLoyaltyProgramEnabled { get; set; } = true;
        public bool IsReviewSystemEnabled { get; set; } = true;
        public bool IsComplaintSystemEnabled { get; set; } = true;

        // Points System
        public int PointsPerDollarSpent { get; set; } = 1;
        public int PointsForReview { get; set; } = 50;
        public int PointsForReferral { get; set; } = 100;
        public decimal PointsRedemptionValue { get; set; } = 0.01m; // $0.01 per point

        // Tier Requirements (number of purchases)
        public int BronzeRequirement { get; set; } = 0;
        public int SilverRequirement { get; set; } = 10;
        public int GoldRequirement { get; set; } = 30;
        public int PlatinumRequirement { get; set; } = 50;
        public int DiamondRequirement { get; set; } = 100;

        // Tier Discounts (percentage)
        public decimal BronzeDiscount { get; set; } = 0;
        public decimal SilverDiscount { get; set; } = 5;
        public decimal GoldDiscount { get; set; } = 10;
        public decimal PlatinumDiscount { get; set; } = 15;
        public decimal DiamondDiscount { get; set; } = 20;

        // Milestone Rewards (JSON: purchases -> points)
        public bool EnableMilestoneRewards { get; set; } = true;

        [StringLength(1000)]
        public string MilestoneRewardsJson { get; set; } = "{\"5\":100,\"10\":250,\"25\":500,\"50\":1000,\"100\":2500}";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}