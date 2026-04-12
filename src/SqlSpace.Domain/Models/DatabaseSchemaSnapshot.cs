using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SqlSpace.Domain.Models;

public class DatabaseSchemaSnapshot
{
    public Guid SnapshotId {get ; set; }
    public Guid DatabaseConnectionId {get ; set; } 
    public string SchemaText {get ; set; } = string.Empty;
    public bool IsLatest {get;set;}
    public  DateTime CapturedAt  {get;set;}
    public string SchemaHash {get;set;} = string.Empty;

    public ConnectedDatabase DatabaseConnection { get; set; } = null!;
    
}
