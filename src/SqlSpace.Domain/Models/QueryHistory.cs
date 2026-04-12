using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqlSpace.Domain.Enums;

namespace SqlSpace.Domain.Models;

public class QueryHistory
{
    
    public Guid QueryId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid DatabaseConnectionId { get; set; }  

    public string UserPrompt { get; set; } = string.Empty;
    public string GeneratedSql { get; set; } = string.Empty;
    public string? LlmResponse { get; set; }
    public QueryStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ResultsJson { get; set; }
    public int? RowsReturned { get; set; }
    public long? ExecutionTimeMs { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public string? AccessibleTablesSnapshot { get; set; }
    public string? RestrictedTablesSnapshot { get; set; }
    public bool WasAdminAtExecution { get; set; } = false;

   

    public ConnectedDatabase DatabaseConnection { get; set; } = null!;



}
