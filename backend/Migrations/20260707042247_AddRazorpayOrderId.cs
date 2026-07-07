using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logistics.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRazorpayOrderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "razorpay_order_id",
                table: "payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "razorpay_payment_id",
                table: "payments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "razorpay_order_id",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "razorpay_payment_id",
                table: "payments");
        }
    }
}
