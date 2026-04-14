using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqlSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OriginalPrompt = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK_Reports_ConnectedDatabases_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "ConnectedDatabases",
                        principalColumn: "ConnectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportSections",
                columns: table => new
                {
                    SectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Heading = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NarrativeText = table.Column<string>(type: "text", nullable: false),
                    ChartType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ChartConfigJson = table.Column<string>(type: "text", nullable: true),
                    SqlQuery = table.Column<string>(type: "text", nullable: true),
                    CachedResultsJson = table.Column<string>(type: "jsonb", nullable: true),
                    CachedResultsRowsReturned = table.Column<int>(type: "integer", nullable: true),
                    CachedResultsExecutionTimeMs = table.Column<long>(type: "bigint", nullable: true),
                    CachedResultsSuccess = table.Column<bool>(type: "boolean", nullable: true),
                    CachedResultsErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CachedResultsExecutedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSections", x => x.SectionId);
                    table.ForeignKey(
                        name: "FK_ReportSections_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "ReportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ConnectionId_UserId_CreatedAtUtc",
                table: "Reports",
                columns: new[] { "ConnectionId", "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportSections_ReportId",
                table: "ReportSections",
                column: "ReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportSections");

            migrationBuilder.DropTable(
                name: "Reports");
        }
    }
}
