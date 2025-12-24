using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroGrocery.Migrations
{
    /// <inheritdoc />
    public partial class FixShipmentOneToOne_Clean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. First, drop the foreign key (this also drops the index automatically in MySQL)
            migrationBuilder.DropForeignKey(
                name: "FK_Shipment_Order",
                table: "shipments");

            // 2. Drop the old index if it still exists (defensive)
            migrationBuilder.DropIndex(
                name: "IX_shipments_OrderId",
                table: "shipments");

            // 3. Make OrderId unique (required for true one-to-one)
            migrationBuilder.CreateIndex(
                name: "IX_shipments_OrderId",
                table: "shipments",
                column: "OrderId",
                unique: true);

            // 4. Re-add the foreign key with the same name
            migrationBuilder.AddForeignKey(
                name: "FK_Shipment_Order",
                table: "shipments",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipment_Order",
                table: "shipments");

            migrationBuilder.DropIndex(
                name: "IX_shipments_OrderId",
                table: "shipments");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_OrderId",
                table: "shipments",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipment_Order",
                table: "shipments",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
