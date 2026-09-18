using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiMatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveTripCargoUniqueRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_trip_cargos_TransportRequestId",
                table: "trip_cargos");

            migrationBuilder.DropIndex(
                name: "IX_trip_cargos_TripId_TransportRequestId",
                table: "trip_cargos");

            migrationBuilder.CreateIndex(
                name: "IX_trip_cargos_TransportRequestId",
                table: "trip_cargos",
                column: "TransportRequestId",
                unique: true,
                filter: "\"Status\" <> 4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_trip_cargos_TransportRequestId",
                table: "trip_cargos");

            migrationBuilder.CreateIndex(
                name: "IX_trip_cargos_TransportRequestId",
                table: "trip_cargos",
                column: "TransportRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_trip_cargos_TripId_TransportRequestId",
                table: "trip_cargos",
                columns: new[] { "TripId", "TransportRequestId" },
                unique: true);
        }
    }
}
