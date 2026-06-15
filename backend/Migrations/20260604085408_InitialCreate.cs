using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logistics.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auth0id = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "disputes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    raised_by = table.Column<Guid>(type: "uuid", nullable: false),
                    complaint_text = table.Column<string>(type: "text", nullable: false),
                    llm_summary = table.Column<string>(type: "text", nullable: true),
                    llm_type = table.Column<string>(type: "text", nullable: true),
                    llm_suggested_resolution = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    resolved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    resolution_notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_disputes", x => x.id);
                    table.ForeignKey(
                        name: "fk_disputes_users_raised_by",
                        column: x => x.raised_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_disputes_users_resolved_by",
                        column: x => x.resolved_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "drivers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    license_number = table.Column<string>(type: "text", nullable: false),
                    approval_status = table.Column<string>(type: "text", nullable: false),
                    operational_status = table.Column<string>(type: "text", nullable: false),
                    active_vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    current_lat = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    current_lng = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    last_ping_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approval_reason = table.Column<string>(type: "text", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancel_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_drivers", x => x.id);
                    table.ForeignKey(
                        name: "fk_drivers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_type = table.Column<string>(type: "text", nullable: false),
                    vehicle_number = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicles", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicles_drivers_driver_id",
                        column: x => x.driver_id,
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shipments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<string>(type: "text", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    pickup_address = table.Column<string>(type: "text", nullable: false),
                    pickup_lat = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    pickup_lng = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    drop_address = table.Column<string>(type: "text", nullable: false),
                    drop_lat = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    drop_lng = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    receiver_name = table.Column<string>(type: "text", nullable: false),
                    receiver_phone = table.Column<string>(type: "text", nullable: false),
                    package_type = table.Column<string>(type: "text", nullable: false),
                    weight_kg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    preferred_window = table.Column<string>(type: "text", nullable: true),
                    special_notes = table.Column<string>(type: "text", nullable: true),
                    risk_flag = table.Column<bool>(type: "boolean", nullable: false),
                    risk_severity = table.Column<string>(type: "text", nullable: false),
                    risk_reason = table.Column<string>(type: "text", nullable: true),
                    preferred_delivery_after = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    driver_instruction = table.Column<string>(type: "text", nullable: true),
                    sender_otp_hash = table.Column<string>(type: "text", nullable: true),
                    sender_otp_attempts = table.Column<int>(type: "integer", nullable: false),
                    sender_otp_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    receiver_otp_hash = table.Column<string>(type: "text", nullable: true),
                    receiver_otp_attempts = table.Column<int>(type: "integer", nullable: false),
                    receiver_otp_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cash_collected = table.Column<bool>(type: "boolean", nullable: false),
                    status_changed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    status_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipments", x => x.id);
                    table.ForeignKey(
                        name: "fk_shipments_drivers_driver_id",
                        column: x => x.driver_id,
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_shipments_users_customer_id",
                        column: x => x.customer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_shipments_users_status_changed_by",
                        column: x => x.status_changed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_shipments_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_shipments_shipment_id",
                        column: x => x.shipment_id,
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    idempotency_key = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_payments_shipments_shipment_id",
                        column: x => x.shipment_id,
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tracking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tracking", x => x.id);
                    table.ForeignKey(
                        name: "fk_tracking_drivers_driver_id",
                        column: x => x.driver_id,
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tracking_shipments_shipment_id",
                        column: x => x.shipment_id,
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_disputes_raised_by",
                table: "disputes",
                column: "raised_by");

            migrationBuilder.CreateIndex(
                name: "ix_disputes_resolved_by",
                table: "disputes",
                column: "resolved_by");

            migrationBuilder.CreateIndex(
                name: "ix_disputes_shipment_id",
                table: "disputes",
                column: "shipment_id");

            migrationBuilder.CreateIndex(
                name: "ix_drivers_active_vehicle_id",
                table: "drivers",
                column: "active_vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_drivers_operational_status",
                table: "drivers",
                column: "operational_status");

            migrationBuilder.CreateIndex(
                name: "ix_drivers_user_id",
                table: "drivers",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_shipment_id",
                table: "notifications",
                column: "shipment_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_is_read",
                table: "notifications",
                columns: new[] { "user_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_idempotency_key",
                table: "payments",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_shipment_id",
                table: "payments",
                column: "shipment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shipments_customer_id",
                table: "shipments",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_driver_id",
                table: "shipments",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_order_id",
                table: "shipments",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shipments_status",
                table: "shipments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_status_changed_by",
                table: "shipments",
                column: "status_changed_by");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_vehicle_id",
                table: "shipments",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_tracking_driver_id",
                table: "tracking",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_tracking_shipment_id_recorded_at",
                table: "tracking",
                columns: new[] { "shipment_id", "recorded_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_users_auth0id",
                table: "users",
                column: "auth0id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_users_phone",
                table: "users",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_driver_id",
                table: "vehicles",
                column: "driver_id");

            migrationBuilder.AddForeignKey(
                name: "fk_disputes_shipments_shipment_id",
                table: "disputes",
                column: "shipment_id",
                principalTable: "shipments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_drivers_vehicles_active_vehicle_id",
                table: "drivers",
                column: "active_vehicle_id",
                principalTable: "vehicles",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_drivers_users_user_id",
                table: "drivers");

            migrationBuilder.DropForeignKey(
                name: "fk_drivers_vehicles_active_vehicle_id",
                table: "drivers");

            migrationBuilder.DropTable(
                name: "disputes");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "tracking");

            migrationBuilder.DropTable(
                name: "shipments");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "vehicles");

            migrationBuilder.DropTable(
                name: "drivers");
        }
    }
}
