using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiMatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTripCargo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "trip_cargos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransportRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    VolumeM3 = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trip_cargos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_trip_cargos_transport_requests_TransportRequestId",
                        column: x => x.TransportRequestId,
                        principalTable: "transport_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trip_cargos_trips_TripId",
                        column: x => x.TripId,
                        principalTable: "trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_trip_cargos_TransportRequestId",
                table: "trip_cargos",
                column: "TransportRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_trip_cargos_TripId",
                table: "trip_cargos",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_trip_cargos_TripId_TransportRequestId",
                table: "trip_cargos",
                columns: new[] { "TripId", "TransportRequestId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trip_cargos");
        }
    }
}
