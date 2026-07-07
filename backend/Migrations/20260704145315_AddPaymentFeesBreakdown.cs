using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logistics.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentFeesBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "cgst",
                table: "payments",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "delivery_charge",
                table: "payments",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "driver_commission",
                table: "payments",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "driver_earnings",
                table: "payments",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "platform_fee",
                table: "payments",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "sgst",
                table: "payments",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cgst",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "delivery_charge",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "driver_commission",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "driver_earnings",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "platform_fee",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "sgst",
                table: "payments");
        }
    }
}
