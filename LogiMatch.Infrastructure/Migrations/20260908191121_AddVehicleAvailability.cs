using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiMatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vehicle_availabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AvailableFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AvailableTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_availabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicle_availabilities_vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_availabilities_AvailableFrom",
                table: "vehicle_availabilities",
                column: "AvailableFrom");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_availabilities_AvailableTo",
                table: "vehicle_availabilities",
                column: "AvailableTo");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_availabilities_VehicleId",
                table: "vehicle_availabilities",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vehicle_availabilities");
        }
    }
}
