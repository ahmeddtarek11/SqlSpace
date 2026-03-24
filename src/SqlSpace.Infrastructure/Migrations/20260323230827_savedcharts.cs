using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqlSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class savedcharts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedCharts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DatabaseConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SqlQuery = table.Column<string>(type: "text", nullable: false),
                    OriginalPrompt = table.Column<string>(type: "text", nullable: true),
                    ChartType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChartConfigJson = table.Column<string>(type: "text", nullable: false),
                    GridX = table.Column<int>(type: "integer", nullable: false),
                    GridY = table.Column<int>(type: "integer", nullable: false),
                    GridW = table.Column<int>(type: "integer", nullable: false),
                    GridH = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedCharts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedCharts_ConnectedDatabases_DatabaseConnectionId",
                        column: x => x.DatabaseConnectionId,
                        principalTable: "ConnectedDatabases",
                        principalColumn: "ConnectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedCharts_DatabaseConnectionId",
                table: "SavedCharts",
                column: "DatabaseConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedCharts_UserId_DatabaseConnectionId",
                table: "SavedCharts",
                columns: new[] { "UserId", "DatabaseConnectionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedCharts");
        }
    }
}
