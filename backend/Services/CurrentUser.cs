using System;
using System.Linq;
using System.Security.Claims;
using Logistics.Api.Data;
using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _db;

    public CurrentUser(IHttpContextAccessor httpContextAccessor, AppDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    public Guid Id
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.User?.Identity?.IsAuthenticated == true)
            {
                var nameId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(nameId))
                {
                    if (context.Items.TryGetValue("DbUserId", out var cachedId) && cachedId is Guid guidId)
                    {
                        return guidId;
                    }

                    var user = _db.Users.AsNoTracking().FirstOrDefault(u => u.Auth0Id == nameId);
                    if (user != null)
                    {
                        context.Items["DbUserId"] = user.Id;
                        return user.Id;
                    }
                }
            }

            return Guid.Empty;
        }
    }

    public string Role
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.User
                .FindFirst("https://logistics.api/claims/roles")?.Value
                ?? string.Empty;
        }
    }

    public Guid? DriverId
    {
        get
        {
            var userId = Id; 
            if (userId == Guid.Empty) return null;

            var context = _httpContextAccessor.HttpContext;
            if (context?.Items.TryGetValue("DbDriverId", out var cachedId) == true && cachedId is Guid guidId)
            {
                return guidId;
            }

            var driver = _db.Drivers.AsNoTracking().FirstOrDefault(d => d.UserId == userId);
            if (driver != null)
            {
                if (context != null)
                    context.Items["DbDriverId"] = driver.Id;
                return driver.Id;
            }

            return null;
        }
    }
}
