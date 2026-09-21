using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiMatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyOwnerUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "companies",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.CreateIndex(
                name: "IX_companies_OwnerUserId",
                table: "companies",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_companies_users_OwnerUserId",
                table: "companies",
                column: "OwnerUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_companies_users_OwnerUserId",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "IX_companies_OwnerUserId",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "companies");
        }
    }
}
