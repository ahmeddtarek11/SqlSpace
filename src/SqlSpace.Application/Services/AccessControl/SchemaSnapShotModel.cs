using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SqlSpace.Application.Services.AccessControl;

public class SchemaSnapShotModel 
{
    
 public string Database { get; set; } = string.Empty;
public List<TableInfo> Tables { get; set; } = new List<TableInfo>();
}

public class TableInfo
{
    public string Schema { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    // ... other properties
}
