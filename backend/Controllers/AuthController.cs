using Logistics.Api.DTOs;
using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Logistics.Api.Data;
using Logistics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _db;

    public AuthController(IAuthService authService, AppDbContext db)
    {
        _authService = authService;
        _db = db;
    }

    [HttpPost("register/customer")]
    public async Task<IActionResult> RegisterCustomer(RegisterCustomerRequest request)
    {
        var result = await _authService.RegisterCustomerAsync(request);
        return Ok(result);
    }

    [HttpPost("register/driver")]
    public async Task<IActionResult> RegisterDriver(RegisterDriverRequest request)
    {
        var result = await _authService.RegisterDriverAsync(request);
        return Ok(result);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var auth0Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(auth0Id))
        {
            return Unauthorized(new { message = "Auth0 ID not found in token." });
        }

        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Driver)
            .FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

        if (user == null)
        {
            return Ok(new { isAuthenticated = true, isRegistered = false });
        }

        object? driverDetails = null;
        if (user.Role == UserRole.DRIVER && user.Driver != null)
        {
            driverDetails = new
            {
                approvalStatus = user.Driver.ApprovalStatus,
                approvalReason = user.Driver.ApprovalReason,
                operationalStatus = user.Driver.OperationalStatus,
                activeVehicleId = user.Driver.ActiveVehicleId
            };
        }

        return Ok(new
        {
            isAuthenticated = true,
            isRegistered = true,
            userId = user.Id,
            fullName = user.FullName,
            email = user.Email,
            role = user.Role.ToString(),
            driver = driverDetails
        });
    }

    [HttpGet("debug")]
    public IActionResult DebugAuth()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        return Ok(new
        {
            IsAuthenticated = User.Identity?.IsAuthenticated,
            AuthenticationType = User.Identity?.AuthenticationType,
            Claims = claims
        });
    }
}
