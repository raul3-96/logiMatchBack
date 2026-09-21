using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiMatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransportOffer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transport_offers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransportRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransporterProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    EstimatedPickupDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EstimatedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transport_offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transport_offers_transport_requests_TransportRequestId",
                        column: x => x.TransportRequestId,
                        principalTable: "transport_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_transport_offers_transporter_profiles_TransporterProfileId",
                        column: x => x.TransporterProfileId,
                        principalTable: "transporter_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transport_offers_vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_transport_offers_Status",
                table: "transport_offers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_transport_offers_TransporterProfileId",
                table: "transport_offers",
                column: "TransporterProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_transport_offers_TransportRequestId",
                table: "transport_offers",
                column: "TransportRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_transport_offers_VehicleId",
                table: "transport_offers",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transport_offers");
        }
    }
}
