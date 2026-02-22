using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using SqlSpace.Application.Abstractions.Auth;

namespace SqlSpace.Api.common;

public class CurrentUserService : ICurrentUserService
{
    private readonly HttpContext? _httpContext;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContext = httpContextAccessor.HttpContext!;
        
    }
    

    public string? GetClientIpAddress()
    {
      if (_httpContext == null) return null;
        
        var forwarded = _httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        return forwarded?.Split(',')[0].Trim() 
            ?? _httpContext.Connection.RemoteIpAddress?.ToString();
    }

    public string? GetUserEmail()
    {
        var emailClaim =  _httpContext?.User.FindFirst(ClaimTypes.Email);
        if(emailClaim is null)
        {
            emailClaim = _httpContext?.User.FindFirst("email");
        }
        return emailClaim!.Value;
    }

    public string? GetUserId()
    {
       var userId = _httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId;
    }

    public string? GetUserName()
    {
       return _httpContext?.User.Identity?.Name 
            ?? _httpContext?.User.FindFirstValue(ClaimTypes.Name);
    }

    public IReadOnlyCollection<string> GetUserRoles()
    {
        var roles = _httpContext?.User.FindAll(ClaimTypes.Role)
                                    .Select(r => r.Value)
                                    .ToList();
    return roles ?? new List<string>();
    }

    public bool IsAuthenticated()
    {
        return _httpContext?.User.Identity?.IsAuthenticated ?? false;
    }
}
