using System;
using System.Threading.Tasks;
using Logistics.Api.DTOs;
using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/tracking")]
[Authorize]
public class TrackingController : ControllerBase
{
    private readonly ITrackingService _trackingService;
    private readonly ICurrentUser _currentUser;

    public TrackingController(ITrackingService trackingService, ICurrentUser currentUser)
    {
        _trackingService = trackingService;
        _currentUser = currentUser;
    }

    [HttpPost("location")]
    [Authorize(Roles = "DRIVER")]
    public async Task<IActionResult> RecordLocation(TrackingLocationRequest request)
    {
        await _trackingService.RecordLocationAsync(request, _currentUser.Id);
        return Ok();
    }

    [HttpGet("{shipmentId:guid}/live")]
    public async Task<IActionResult> GetLiveLocation(Guid shipmentId)
    {
        var result = await _trackingService.GetLiveLocationAsync(shipmentId, _currentUser.Id, _currentUser.Role);
        return Ok(result);
    }

    [HttpGet("{shipmentId:guid}/history")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetHistory(Guid shipmentId)
    {
        var result = await _trackingService.GetHistoryAsync(shipmentId, _currentUser.Role);
        return Ok(result);
    }
}
