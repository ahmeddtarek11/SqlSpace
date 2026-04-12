using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SqlSpace.Domain.Models;

public class RefreshToken
{
    public Guid Id {get;set;}
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset ExpiresOnUtc { get; set; }
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    
}
