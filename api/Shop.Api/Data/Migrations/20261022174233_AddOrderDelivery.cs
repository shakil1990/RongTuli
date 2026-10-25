using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shop.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderDelivery : Migration
    {
        /// <inheritdoc />
        // Rows from before this migration get the inside-Dhaka zone and a fee of 0: they were
        // placed with no delivery choice, and changing what they charged would rewrite history.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Size",
                table: "OrderItem",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryFee",
                table: "Order",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryZone",
                table: "Order",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "InsideDhaka");

            migrationBuilder.CreateIndex(
                name: "IX_Order_Phone_CreatedAt",
                table: "Order",
                columns: new[] { "Phone", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Order_Phone_CreatedAt",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "DeliveryFee",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "DeliveryZone",
                table: "Order");

            migrationBuilder.AlterColumn<string>(
                name: "Size",
                table: "OrderItem",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }
    }
}
