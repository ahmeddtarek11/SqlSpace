using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SqlSpace.Domain.Models;

public class UserDatabaseAccess
{
     public int Id { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    
    
    public Guid DatabaseConnectionId { get; set; }
    public  ConnectedDatabase DatabaseConnection { get; set; } = null!;
    
    public bool HasFullAccess { get; set; } = false;
    
 
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    
    public string GrantedByUserId { get; set; } = string.Empty;
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? RevokedAt { get; set; }
 
    public string? RevokedByUserId { get; set; }
    
    public virtual ICollection<TableRestriction> TableRestrictions { get; set; } = new List<TableRestriction>();
}
