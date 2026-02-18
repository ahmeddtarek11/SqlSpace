using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqlSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DataBaseNameCloumnAddition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DatabaseName",
                table: "ConnectedDatabases",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DatabaseName",
                table: "ConnectedDatabases");
        }
    }
}
