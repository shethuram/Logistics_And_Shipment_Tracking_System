using Logistics.Api.DTOs;
using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register/customer")]
    public async Task<IActionResult> RegisterCustomer(RegisterCustomerRequest request)
    {
        var result = await _authService.RegisterCustomerAsync(request);
        return CreatedAtAction(nameof(RegisterCustomer), new { id = result.Id }, result);
    }

    [HttpPost("register/driver")]
    public async Task<IActionResult> RegisterDriver(RegisterDriverRequest request)
    {
        var result = await _authService.RegisterDriverAsync(request);
        return CreatedAtAction(nameof(RegisterDriver), new { id = result.Id }, result);
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
