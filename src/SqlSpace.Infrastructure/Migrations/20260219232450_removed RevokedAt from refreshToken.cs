using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqlSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class removedRevokedAtfromrefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_RevokedOnUtc",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "RevokedOnUtc",
                table: "RefreshTokens");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RevokedOnUtc",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_RevokedOnUtc",
                table: "RefreshTokens",
                columns: new[] { "UserId", "RevokedOnUtc" });
        }
    }
}
