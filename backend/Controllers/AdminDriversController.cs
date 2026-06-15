using Logistics.Api.DTOs;
using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/admin/drivers")]
[Authorize(Roles = "ADMIN")]
public class AdminDriversController : ControllerBase
{
    private readonly IAdminDriverService _adminDriverService;

    public AdminDriversController(IAdminDriverService adminDriverService)
    {
        _adminDriverService = adminDriverService;
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
}
