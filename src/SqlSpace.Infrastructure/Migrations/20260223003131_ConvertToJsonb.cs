using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqlSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertToJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
{
    // Convert QueryHistories columns to jsonb
    migrationBuilder.Sql(@"
        ALTER TABLE ""QueryHistories"" 
        ALTER COLUMN ""ResultsJson"" 
        TYPE jsonb 
        USING CASE 
            WHEN ""ResultsJson"" IS NULL OR ""ResultsJson"" = '' THEN NULL 
            ELSE ""ResultsJson""::jsonb 
        END;
    ");
    
    migrationBuilder.Sql(@"
        ALTER TABLE ""QueryHistories"" 
        ALTER COLUMN ""AccessibleTablesSnapshot"" 
        TYPE jsonb 
        USING CASE 
            WHEN ""AccessibleTablesSnapshot"" IS NULL OR ""AccessibleTablesSnapshot"" = '' THEN NULL 
            ELSE ""AccessibleTablesSnapshot""::jsonb 
        END;
    ");
    
    migrationBuilder.Sql(@"
        ALTER TABLE ""QueryHistories"" 
        ALTER COLUMN ""RestrictedTablesSnapshot"" 
        TYPE jsonb 
        USING CASE 
            WHEN ""RestrictedTablesSnapshot"" IS NULL OR ""RestrictedTablesSnapshot"" = '' THEN NULL 
            ELSE ""RestrictedTablesSnapshot""::jsonb 
        END;
    ");
    
    // Convert DatabaseSchemaSnapshots.SchemaText to jsonb
    migrationBuilder.Sql(@"
        ALTER TABLE ""DatabaseSchemaSnapshots"" 
        ALTER COLUMN ""SchemaText"" 
        TYPE jsonb 
        USING ""SchemaText""::jsonb;
    ");
    
    // Convert AccessAuditLogs.Details to jsonb
    migrationBuilder.Sql(@"
        ALTER TABLE ""AccessAuditLogs"" 
        ALTER COLUMN ""Details"" 
        TYPE jsonb 
        USING CASE 
            WHEN ""Details"" IS NULL OR ""Details"" = '' THEN NULL 
            ELSE ""Details""::jsonb 
        END;
    ");
    
    // Create GIN indexes for fast JSON queries
    migrationBuilder.Sql(@"
        CREATE INDEX ""IX_QueryHistories_ResultsJson"" 
        ON ""QueryHistories"" USING gin(""ResultsJson"") 
        WHERE ""ResultsJson"" IS NOT NULL;
    ");
    
    migrationBuilder.Sql(@"
        CREATE INDEX ""IX_DatabaseSchemaSnapshots_SchemaText"" 
        ON ""DatabaseSchemaSnapshots"" USING gin(""SchemaText"");
    ");
}

        /// <inheritdoc />
       protected override void Down(MigrationBuilder migrationBuilder)
{
    // Drop GIN indexes
    migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_QueryHistories_ResultsJson"";");
    migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_DatabaseSchemaSnapshots_SchemaText"";");
    
    // Convert back to text
    migrationBuilder.Sql(@"
        ALTER TABLE ""QueryHistories"" 
        ALTER COLUMN ""ResultsJson"" TYPE text 
        USING ""ResultsJson""::text;
    ");
    
    migrationBuilder.Sql(@"
        ALTER TABLE ""QueryHistories"" 
        ALTER COLUMN ""AccessibleTablesSnapshot"" TYPE text 
        USING ""AccessibleTablesSnapshot""::text;
    ");
    
    migrationBuilder.Sql(@"
        ALTER TABLE ""QueryHistories"" 
        ALTER COLUMN ""RestrictedTablesSnapshot"" TYPE text 
        USING ""RestrictedTablesSnapshot""::text;
    ");
    
    migrationBuilder.Sql(@"
        ALTER TABLE ""DatabaseSchemaSnapshots"" 
        ALTER COLUMN ""SchemaText"" TYPE text 
        USING ""SchemaText""::text;
    ");
    
    migrationBuilder.Sql(@"
        ALTER TABLE ""AccessAuditLogs"" 
        ALTER COLUMN ""Details"" TYPE text 
        USING ""Details""::text;
    ");
}
    }
}
