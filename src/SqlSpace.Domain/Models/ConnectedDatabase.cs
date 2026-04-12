using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using SqlSpace.Domain.Enums;

namespace SqlSpace.Domain.Models;

public class ConnectedDatabase
{
    public Guid ConnectionId { get; set; }
    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;
    [Required]
    public string DbAdminId    { get; set; } = string.Empty;   

    [Required]
    public string ConnectionName { get; set; }  = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string? Host { get; set; }
    public int? PortNumber { get; set; }
    public DbProviders DatabaseProvider { get; set; }   //builder.Property(x => x.DatabaseProvider).HasConversion<string>();
       
    public string? Username { get; set; }
    public string? EncryptedPassword { get; set; }
   
    public string? AdditionalParameters { get; set; }
    public string? EncryptedRawConnectionString { get; set; } = string.Empty;
    public bool UseSSL { get; set; } = true;
    public bool UsesRawConnectionString { get; set; } = false;
    public DateTime LastSuccessfulConnection { get; set; } 
    public bool IsHealthy { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // soft deletion
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId   {get;set;}


    // navigation properties
    public  ICollection<UserDatabaseAccess> UserAccesses { get; set; } = new List<UserDatabaseAccess>();
    public  ICollection<DatabaseSchemaSnapshot> SchemaSnapshots { get; set; } = new List<DatabaseSchemaSnapshot>();
    public  ICollection<QueryHistory> Queries { get; set; } = new List<QueryHistory>();
    public  ICollection<AccessAuditLog> AuditLogs { get; set; } = new List<AccessAuditLog>();
    public  ICollection<SavedChart> SavedCharts { get; set; } = new List<SavedChart>();
    public ICollection<KnowledgeDocument> KnowledgeDocuments { get; set; } = new List<KnowledgeDocument>();

}

