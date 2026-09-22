using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiMatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyOwnerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "companies",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_users_email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM users
                    ) THEN
                        RAISE EXCEPTION
                            'No existen usuarios para asignar como propietarios de las empresas';
                    END IF;
                END $$;
            """);

                    migrationBuilder.Sql("""
                UPDATE companies
                SET "OwnerUserId" = (
                    SELECT "Id"
                    FROM users
                    ORDER BY "CreatedAt"
                    LIMIT 1
                )
                WHERE "OwnerUserId" IS NULL;
            """);

                    migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM companies
                        WHERE "OwnerUserId" IS NULL
                    ) THEN
                        RAISE EXCEPTION
                            'No se pudo asignar OwnerUserId a todas las empresas existentes';
                    END IF;
                END $$;
            """);

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerUserId",
                table: "companies",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

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
                name: "UX_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_companies_OwnerUserId",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "companies");
        }
    }
}
