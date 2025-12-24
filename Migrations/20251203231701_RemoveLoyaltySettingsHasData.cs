using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroGrocery.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLoyaltySettingsHasData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "LoyaltySettings",
                keyColumn: "Id",
                keyValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "LoyaltySettings",
                columns: new[] { "Id", "BronzeDiscount", "BronzeRequirement", "CreatedAt", "DiamondDiscount", "DiamondRequirement", "EnableMilestoneRewards", "GoldDiscount", "GoldRequirement", "IsComplaintSystemEnabled", "IsLoyaltyProgramEnabled", "IsReviewSystemEnabled", "MilestoneRewardsJson", "PlatinumDiscount", "PlatinumRequirement", "PointsForReferral", "PointsForReview", "PointsPerDollarSpent", "PointsRedemptionValue", "SilverDiscount", "SilverRequirement", "UpdatedAt" },
                values: new object[] { 1, 0m, 0, new DateTime(2025, 12, 3, 23, 10, 42, 816, DateTimeKind.Utc).AddTicks(735), 20m, 100, true, 10m, 30, true, true, true, "{\"5\":100,\"10\":250,\"25\":500,\"50\":1000,\"100\":2500}", 15m, 50, 100, 50, 1, 0.01m, 5m, 10, null });
        }
    }
}
