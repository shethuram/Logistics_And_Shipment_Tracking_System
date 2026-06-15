using Logistics.Api.DTOs;
using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Controllers;

[ApiController]
public class DisputesController : ControllerBase
{
    private readonly IDisputeService _disputeService;
    private readonly ICurrentUser _currentUser;

    public DisputesController(IDisputeService disputeService, ICurrentUser currentUser)
    {
        _disputeService = disputeService;
        _currentUser = currentUser;
    }

    [HttpPost("api/disputes")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> RaiseDispute(RaiseDisputeRequest request)
    {
        var result = await _disputeService.RaiseDisputeAsync(request, _currentUser.Id);
        return CreatedAtAction(nameof(RaiseDispute), new { id = result.Id }, result);
    }

    [HttpGet("api/admin/disputes")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetDisputes(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _disputeService.GetDisputesAsync(status, page, pageSize, _currentUser.Role);
        return Ok(result);
    }

    [HttpPost("api/admin/disputes/{id:guid}/resolve")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ResolveDispute(Guid id, ResolveDisputeRequest request)
    {
        var result = await _disputeService.ResolveDisputeAsync(id, request, _currentUser.Id, _currentUser.Role);
        return Ok(result);
    }
}
