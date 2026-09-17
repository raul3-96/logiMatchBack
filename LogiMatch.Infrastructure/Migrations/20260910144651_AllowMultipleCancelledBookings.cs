using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiMatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleCancelledBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bookings_TransportRequestId",
                table: "bookings");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_TransportRequestId",
                table: "bookings",
                column: "TransportRequestId",
                unique: true,
                filter: "\"Status\" <> 4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bookings_TransportRequestId",
                table: "bookings");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_TransportRequestId",
                table: "bookings",
                column: "TransportRequestId",
                unique: true);
        }
    }
}
