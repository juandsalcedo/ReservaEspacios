using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReservaEspacios.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTokenRestablecimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpires",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetTokenHash",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PasswordResetTokenHash",
                table: "Users",
                column: "PasswordResetTokenHash",
                unique: true,
                filter: "[PasswordResetTokenHash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_PasswordResetTokenHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpires",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenHash",
                table: "Users");
        }
    }
}
