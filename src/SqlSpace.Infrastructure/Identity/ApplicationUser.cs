using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using SqlSpace.Domain.Models;

namespace SqlSpace.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
     
    
    /// Database connections owned by this user (as admin)
    
    public  ICollection<ConnectedDatabase> OwnedConnections { get; set; } = new List<ConnectedDatabase>();
    
    
    /// Database access granted to this user
    
    public  ICollection<UserDatabaseAccess> AccessibleDatabases { get; set; } = new List<UserDatabaseAccess>();
    
    
    /// Queries executed by this user
    
    public  ICollection<QueryHistory> QueriesHistory { get; set; } = new List<QueryHistory>();
    
    
    /// Audit logs where this user performed actions
    
    public  ICollection<AccessAuditLog> AuditLogsAsActor { get; set; } = new List<AccessAuditLog>();
    
    
    /// Audit logs where this user was affected
    
    public  ICollection<AccessAuditLog> AuditLogsAsTarget { get; set; } = new List<AccessAuditLog>();
    
}
