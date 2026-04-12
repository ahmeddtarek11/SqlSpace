using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using SqlSpace.Domain.Models;

namespace SqlSpace.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
     
    
    
    
    public  ICollection<ConnectedDatabase> OwnedConnections { get; set; } = new List<ConnectedDatabase>();
    
    
    
    
    public  ICollection<UserDatabaseAccess> AccessibleDatabases { get; set; } = new List<UserDatabaseAccess>();
    
    
    
    
    public  ICollection<QueryHistory> QueriesHistory { get; set; } = new List<QueryHistory>();
    
    
   
    
    public  ICollection<AccessAuditLog> AuditLogsAsActor { get; set; } = new List<AccessAuditLog>();
    
    
 
    
    public  ICollection<AccessAuditLog> AuditLogsAsTarget { get; set; } = new List<AccessAuditLog>();
    
}
