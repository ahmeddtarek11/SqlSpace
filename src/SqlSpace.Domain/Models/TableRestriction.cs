using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SqlSpace.Domain.Models;

public class TableRestriction
{
    public int Id { get; set; }
    
    public int UserDatabaseAccessId { get; set; }
    public virtual UserDatabaseAccess UserDatabaseAccess { get; set; } = null!;
    
    public string TableName { get; set; } = string.Empty;
    
    /// <summary>
    /// Schema name (e.g., "dbo", "public")
    /// </summary>
    public string? SchemaName { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
