using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqlSpace.Domain.Enums;

namespace SqlSpace.Application.Abstractions.ConnectionManagement.Dtos;

public class ConnectionCreationResponse
{
    public string ConnectionName { get; set; } = string.Empty;
    public Guid ConnectionId { get; set; } 
    public DbProviders Provider { get; set; }

   
    public string AdminId  { get; set; } = string.Empty;   
    public string ConnectionAdminName { get; set; } = string.Empty;
    public string? ConnectionCreatorName { get; set; }  =  string.Empty;
     public string DatabaseName { get; set; } = string.Empty;
      public bool UseSSL { get; set; } = true;
      public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
