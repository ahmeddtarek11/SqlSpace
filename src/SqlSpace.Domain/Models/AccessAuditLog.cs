using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqlSpace.Domain.Enums;

namespace SqlSpace.Domain.Models;

public class AccessAuditLog
{
   public  Guid AuditLogId   {get;set;}
   public  Guid  DatabaseConnectionId {get;set;}
   public  string ActorUserId {get;set;} = string.Empty; 
   public  string TargetUserId {get;set;} = string.Empty; 
   public  AccessAuditLogActions Action {get;set;} 
   public  string?  Details {get;set;} = string.Empty;
   public  DateTime PerformedAt {get;set;} 
   public ConnectedDatabase DatabaseConnection { get; set; } = null!;
   
}
