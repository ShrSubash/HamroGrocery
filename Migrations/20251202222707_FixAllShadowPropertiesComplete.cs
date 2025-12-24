using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HamroGrocery.Migrations
{
    /// <inheritdoc />
    public partial class FixAllShadowPropertiesComplete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_items_product_variations_ProductVariationId1",
                table: "order_items");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_order_items_product_variations_ProductVariationId1",
                table: "purchase_order_items");

            migrationBuilder.DropForeignKey(
                name: "FK_shipments_orders_OrderId",
                table: "shipments");

            migrationBuilder.DropForeignKey(
                name: "FK_shipments_shipping_providers_ShippingProviderId",
                table: "shipments");

            migrationBuilder.DropForeignKey(
                name: "FK_users_cities_CityId1",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "FK_users_countries_CountryId1",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "FK_users_state_provinces_StateProvinceId1",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_CityId1",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_CountryId1",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_StateProvinceId1",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_purchase_order_items_ProductVariationId1",
                table: "purchase_order_items");

            migrationBuilder.DropIndex(
                name: "IX_order_items_ProductVariationId1",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "CityId1",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CountryId1",
                table: "users");

            migrationBuilder.DropColumn(
                name: "StateProvinceId1",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ProductVariationId1",
                table: "purchase_order_items");

            migrationBuilder.DropColumn(
                name: "ProductVariationId1",
                table: "order_items");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipment_Order",
                table: "shipments",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Shipment_ShippingProvider",
                table: "shipments",
                column: "ShippingProviderId",
                principalTable: "shipping_providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipment_Order",
                table: "shipments");

            migrationBuilder.DropForeignKey(
                name: "FK_Shipment_ShippingProvider",
                table: "shipments");

            migrationBuilder.AddColumn<int>(
                name: "CityId1",
                table: "users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CountryId1",
                table: "users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StateProvinceId1",
                table: "users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductVariationId1",
                table: "purchase_order_items",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductVariationId1",
                table: "order_items",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_CityId1",
                table: "users",
                column: "CityId1");

            migrationBuilder.CreateIndex(
                name: "IX_users_CountryId1",
                table: "users",
                column: "CountryId1");

            migrationBuilder.CreateIndex(
                name: "IX_users_StateProvinceId1",
                table: "users",
                column: "StateProvinceId1");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_items_ProductVariationId1",
                table: "purchase_order_items",
                column: "ProductVariationId1");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_ProductVariationId1",
                table: "order_items",
                column: "ProductVariationId1");

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_product_variations_ProductVariationId1",
                table: "order_items",
                column: "ProductVariationId1",
                principalTable: "product_variations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_order_items_product_variations_ProductVariationId1",
                table: "purchase_order_items",
                column: "ProductVariationId1",
                principalTable: "product_variations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_shipments_orders_OrderId",
                table: "shipments",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shipments_shipping_providers_ShippingProviderId",
                table: "shipments",
                column: "ShippingProviderId",
                principalTable: "shipping_providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_cities_CityId1",
                table: "users",
                column: "CityId1",
                principalTable: "cities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_countries_CountryId1",
                table: "users",
                column: "CountryId1",
                principalTable: "countries",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_state_provinces_StateProvinceId1",
                table: "users",
                column: "StateProvinceId1",
                principalTable: "state_provinces",
                principalColumn: "Id");
        }
    }
}
