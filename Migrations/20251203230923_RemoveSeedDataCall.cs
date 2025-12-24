using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroGrocery.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeedDataCall : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LoyaltySettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 3, 23, 9, 23, 18, DateTimeKind.Utc).AddTicks(7579));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LoyaltySettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 3, 23, 7, 5, 876, DateTimeKind.Utc).AddTicks(2279));
        }
    }
}
