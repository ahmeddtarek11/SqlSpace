using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqlSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChatHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnowledgeChatMessages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    SourcesJson = table.Column<string>(type: "jsonb", nullable: true),
                    TokensUsed = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeChatMessages", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_KnowledgeChatMessages_ConnectedDatabases_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "ConnectedDatabases",
                        principalColumn: "ConnectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChatMessages_ConnectionId_UserId_CreatedAt",
                table: "KnowledgeChatMessages",
                columns: new[] { "ConnectionId", "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgeChatMessages");
        }
    }
}
