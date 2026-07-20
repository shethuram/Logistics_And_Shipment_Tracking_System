using Logistics.Api.DTOs;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/admin/drivers")]
[Authorize(Roles = "ADMIN")]
public class AdminDriversController : ControllerBase
{
    private readonly IAdminDriverService _adminDriverService;
    private readonly IConfiguration _configuration;

    public AdminDriversController(IAdminDriverService adminDriverService, IConfiguration configuration)
    {
        _adminDriverService = adminDriverService;
        _configuration = configuration;
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _adminDriverService.GetPendingDriversAsync(page, pageSize);
        return Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _adminDriverService.ApproveDriverAsync(id);
        return Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, RejectDriverRequest request)
    {
        var result = await _adminDriverService.RejectDriverAsync(id, request);
        return Ok(result);
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, SuspendDriverRequest request)
    {
        var result = await _adminDriverService.SuspendDriverAsync(id, request);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetDrivers([FromQuery] ApprovalStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _adminDriverService.GetDriversAsync(status, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _adminDriverService.GetDriverByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPatch("{id:guid}/verification")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateVerification(Guid id, [FromBody] UpdateDriverVerificationRequest request)
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        var expectedKey = _configuration["AgentSettings:ApiKey"];

        if (string.IsNullOrEmpty(expectedKey))
        {
            return StatusCode(500, "API key configuration is missing on the server.");
        }

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ") || authHeader.Substring(7) != expectedKey)
        {
            return Unauthorized("Invalid shared secret authorization token.");
        }

        await _adminDriverService.UpdateDriverVerificationAsync(id, request);
        return Ok();
    }
}
