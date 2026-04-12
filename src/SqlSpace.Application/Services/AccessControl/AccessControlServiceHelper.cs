// Application/Services/AccessControl/RestrictedTablesHelper.cs

using System.Text.Json;
using SqlSpace.Application.Abstractions.Access;
using SqlSpace.Domain.Common.Errors;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Enums;

namespace SqlSpace.Application.Services.AccessControl;

public static class RestrictedTablesHelper
{
    /// <summary>
    /// Checks if a table is in the restricted list.
    /// Expected JSON: [{"schema":"public","table":"customers"}]
    /// </summary>
    public static bool IsTableRestricted(
        this string? restrictedTablesJson,
        DbProviders provider,
        string tableName,
        string? schemaName)
    {
        if (string.IsNullOrWhiteSpace(restrictedTablesJson))
            return false;
        
        var restrictions = JsonSerializer.Deserialize<List<TableRestrictionDto>>(restrictedTablesJson);

        if (restrictions == null || restrictions.Count == 0){
            return false;
        }

        var incomingKey = provider.BuildTableKey(tableName, schemaName);

        return restrictions.Any(r =>
            string.Equals(
                provider.BuildTableKey(r.Table, r.Schema),
                incomingKey,
                StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Gets all restricted tables as a list.
    /// </summary>
    public static List<TableRestrictionDto> GetRestrictedTables(this string? restrictedTablesJson , DbProviders provider)
    {
        if (string.IsNullOrWhiteSpace(restrictedTablesJson))
            return new List<TableRestrictionDto>();
        
        var restrictions =  JsonSerializer.Deserialize<List<TableRestrictionDto>>(restrictedTablesJson) 
            ?? new List<TableRestrictionDto>();

            return restrictions.Select(r => new TableRestrictionDto
            {
                Schema = provider.NormalizeSchema(r.Schema),
                Table = r.Table?.Trim() ?? string.Empty
            }).ToList();
    }





}


