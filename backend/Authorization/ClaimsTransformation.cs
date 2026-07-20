using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using Logistics.Api.Data;
using Logistics.Api.Models;

namespace Logistics.Api.Authorization;

public class UserClaimsDto
{
    public string Role { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? DriverId { get; set; }
}

public class ClaimsTransformation : IClaimsTransformation
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public ClaimsTransformation(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.HasClaim(c => c.Type == "https://logistics.api/claims/user_id"))
            return principal;

        var nameId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(nameId))
            return principal;

        var cacheKey = $"user-claims-{nameId}";
        if (!_cache.TryGetValue(cacheKey, out UserClaimsDto? dto))
        {
            var user = await _db.Users
                .Include(u => u.Driver)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Id == nameId);

            if (user != null && user.IsActive)
            {
                string assignedRole = user.Role.ToString();

                if (user.Role == UserRole.DRIVER && user.Driver != null)
                {
                    if (user.Driver.ApprovalStatus != ApprovalStatus.APPROVED)
                    {
                        assignedRole = $"DRIVER_{user.Driver.ApprovalStatus}";
                    }
                }

                dto = new UserClaimsDto
                {
                    Role = assignedRole,
                    UserId = user.Id.ToString(),
                    DriverId = user.Driver?.Id.ToString()
                };

                // Short 1-second cache for pending/rejected drivers to ensure real-time role updates
                if (user.Role == UserRole.DRIVER && user.Driver != null && user.Driver.ApprovalStatus != ApprovalStatus.APPROVED)
                {
                    _cache.Set(cacheKey, dto, TimeSpan.FromSeconds(1));
                }
                else
                {
                    _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(5));
                }
            }
        }

        if (dto != null)
        {
            var identity = principal.Identity as ClaimsIdentity;
            if (identity != null)
            {
                identity.AddClaim(new Claim("https://logistics.api/claims/roles", dto.Role));
                identity.AddClaim(new Claim("https://logistics.api/claims/user_id", dto.UserId));
                if (!string.IsNullOrEmpty(dto.DriverId))
                {
                    identity.AddClaim(new Claim("https://logistics.api/claims/driver_id", dto.DriverId));
                }
            }
        }

        return principal;
    }
}
