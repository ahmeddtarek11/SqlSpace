using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqlSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class saveQueriestableaddttion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedQueries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DatabaseConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserPrompt = table.Column<string>(type: "text", nullable: false),
                    GeneratedSql = table.Column<string>(type: "text", nullable: false),
                    QueryHistoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedQueries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedQueries_ConnectedDatabases_DatabaseConnectionId",
                        column: x => x.DatabaseConnectionId,
                        principalTable: "ConnectedDatabases",
                        principalColumn: "ConnectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedQueries_DatabaseConnectionId",
                table: "SavedQueries",
                column: "DatabaseConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedQueries_UserId_CreatedAtUtc",
                table: "SavedQueries",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedQueries");
        }
    }
}
