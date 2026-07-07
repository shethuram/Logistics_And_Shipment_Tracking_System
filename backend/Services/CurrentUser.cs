using System;
using System.Security.Claims;
using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace Logistics.Api.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid Id
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            var userIdStr = context?.User?.FindFirst("https://logistics.api/claims/user_id")?.Value;
            if (Guid.TryParse(userIdStr, out var userId))
            {
                return userId;
            }
            return Guid.Empty;
        }
    }

    public string Role
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.User?.FindFirst("https://logistics.api/claims/roles")?.Value ?? string.Empty;
        }
    }

    public Guid? DriverId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            var driverIdStr = context?.User?.FindFirst("https://logistics.api/claims/driver_id")?.Value;
            if (Guid.TryParse(driverIdStr, out var driverId))
            {
                return driverId;
            }
            return null;
        }
    }
}
