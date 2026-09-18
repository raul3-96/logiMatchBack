using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiMatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransportRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransportOfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bookings_transport_offers_TransportOfferId",
                        column: x => x.TransportOfferId,
                        principalTable: "transport_offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bookings_transport_requests_TransportRequestId",
                        column: x => x.TransportRequestId,
                        principalTable: "transport_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_TransportOfferId",
                table: "bookings",
                column: "TransportOfferId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_TransportRequestId",
                table: "bookings",
                column: "TransportRequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookings");
        }
    }
}
