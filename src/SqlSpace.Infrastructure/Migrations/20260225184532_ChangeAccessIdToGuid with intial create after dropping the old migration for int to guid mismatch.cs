using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SqlSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeAccessIdToGuidwithintialcreateafterdroppingtheoldmigrationforinttoguidmismatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ExpiresOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConnectedDatabases",
                columns: table => new
                {
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DbAdminId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ConnectionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DatabaseName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PortNumber = table.Column<int>(type: "integer", nullable: true),
                    DatabaseProvider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Username = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EncryptedPassword = table.Column<string>(type: "text", nullable: true),
                    AdditionalParameters = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EncryptedRawConnectionString = table.Column<string>(type: "text", nullable: true),
                    UseSSL = table.Column<bool>(type: "boolean", nullable: false),
                    UsesRawConnectionString = table.Column<bool>(type: "boolean", nullable: false),
                    LastSuccessfulConnection = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsHealthy = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectedDatabases", x => x.ConnectionId);
                    table.ForeignKey(
                        name: "FK_ConnectedDatabases_AspNetUsers_DbAdminId",
                        column: x => x.DbAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccessAuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    DatabaseConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TargetUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Details = table.Column<string>(type: "jsonb", nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessAuditLogs", x => x.AuditLogId);
                    table.ForeignKey(
                        name: "FK_AccessAuditLogs_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessAuditLogs_AspNetUsers_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessAuditLogs_ConnectedDatabases_DatabaseConnectionId",
                        column: x => x.DatabaseConnectionId,
                        principalTable: "ConnectedDatabases",
                        principalColumn: "ConnectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseSchemaSnapshots",
                columns: table => new
                {
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    DatabaseConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemaText = table.Column<string>(type: "jsonb", nullable: false),
                    IsLatest = table.Column<bool>(type: "boolean", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SchemaHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseSchemaSnapshots", x => x.SnapshotId);
                    table.ForeignKey(
                        name: "FK_DatabaseSchemaSnapshots_ConnectedDatabases_DatabaseConnecti~",
                        column: x => x.DatabaseConnectionId,
                        principalTable: "ConnectedDatabases",
                        principalColumn: "ConnectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QueryHistories",
                columns: table => new
                {
                    QueryId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DatabaseConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserPrompt = table.Column<string>(type: "text", nullable: false),
                    GeneratedSql = table.Column<string>(type: "text", nullable: false),
                    LlmResponse = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ResultsJson = table.Column<string>(type: "jsonb", nullable: true),
                    RowsReturned = table.Column<int>(type: "integer", nullable: true),
                    ExecutionTimeMs = table.Column<long>(type: "bigint", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AccessibleTablesSnapshot = table.Column<string>(type: "jsonb", nullable: true),
                    RestrictedTablesSnapshot = table.Column<string>(type: "jsonb", nullable: true),
                    WasAdminAtExecution = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryHistories", x => x.QueryId);
                    table.ForeignKey(
                        name: "FK_QueryHistories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QueryHistories_ConnectedDatabases_DatabaseConnectionId",
                        column: x => x.DatabaseConnectionId,
                        principalTable: "ConnectedDatabases",
                        principalColumn: "ConnectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserDatabaseAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DatabaseConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    HasFullAccess = table.Column<bool>(type: "boolean", nullable: false),
                    RestrictedTablesJson = table.Column<string>(type: "jsonb", nullable: true),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GrantedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDatabaseAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDatabaseAccesses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserDatabaseAccesses_ConnectedDatabases_DatabaseConnectionId",
                        column: x => x.DatabaseConnectionId,
                        principalTable: "ConnectedDatabases",
                        principalColumn: "ConnectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessAuditLogs_ActorUserId_PerformedAt",
                table: "AccessAuditLogs",
                columns: new[] { "ActorUserId", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessAuditLogs_DatabaseConnectionId_PerformedAt",
                table: "AccessAuditLogs",
                columns: new[] { "DatabaseConnectionId", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessAuditLogs_TargetUserId_PerformedAt",
                table: "AccessAuditLogs",
                columns: new[] { "TargetUserId", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedDatabases_ConnectionName_IsDeleted",
                table: "ConnectedDatabases",
                columns: new[] { "ConnectionName", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedDatabases_CreatedByUserId_IsDeleted",
                table: "ConnectedDatabases",
                columns: new[] { "CreatedByUserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedDatabases_DbAdminId_ConnectionName_IsDeleted",
                table: "ConnectedDatabases",
                columns: new[] { "DbAdminId", "ConnectionName", "IsDeleted" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedDatabases_DbAdminId_IsDeleted",
                table: "ConnectedDatabases",
                columns: new[] { "DbAdminId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseSchemaSnapshots_DatabaseConnectionId_CapturedAt",
                table: "DatabaseSchemaSnapshots",
                columns: new[] { "DatabaseConnectionId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseSchemaSnapshots_DatabaseConnectionId_IsLatest",
                table: "DatabaseSchemaSnapshots",
                columns: new[] { "DatabaseConnectionId", "IsLatest" });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseSchemaSnapshots_DatabaseConnectionId_SchemaHash",
                table: "DatabaseSchemaSnapshots",
                columns: new[] { "DatabaseConnectionId", "SchemaHash" });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseSchemaSnapshots_SchemaText",
                table: "DatabaseSchemaSnapshots",
                column: "SchemaText")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_QueryHistories_DatabaseConnectionId_ExecutedAt",
                table: "QueryHistories",
                columns: new[] { "DatabaseConnectionId", "ExecutedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QueryHistories_ResultsJson",
                table: "QueryHistories",
                column: "ResultsJson")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_QueryHistories_Status",
                table: "QueryHistories",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QueryHistories_UserId_ExecutedAt",
                table: "QueryHistories",
                columns: new[] { "UserId", "ExecutedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDatabaseAccesses_DatabaseConnectionId_UserId_IsDeleted_~",
                table: "UserDatabaseAccesses",
                columns: new[] { "DatabaseConnectionId", "UserId", "IsDeleted", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserDatabaseAccesses_UserId_IsDeleted",
                table: "UserDatabaseAccesses",
                columns: new[] { "UserId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessAuditLogs");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "DatabaseSchemaSnapshots");

            migrationBuilder.DropTable(
                name: "QueryHistories");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "UserDatabaseAccesses");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "ConnectedDatabases");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
