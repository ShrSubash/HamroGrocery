using Microsoft.EntityFrameworkCore.Migrations;

namespace HamroGrocery.Migrations
{
    public partial class FinalizeStaticSeedData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix the only remaining dynamic value (LoyaltySettings CreatedAt)
            migrationBuilder.UpdateData(
                table: "LoyaltySettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LoyaltySettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: DateTime.UtcNow);
        }
    }
}