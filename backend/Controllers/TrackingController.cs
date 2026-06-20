using System;
using System.Threading.Tasks;
using Logistics.Api.DTOs;
using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Logistics.Api.Exceptions;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/tracking")]
[Authorize]
public class TrackingController : ControllerBase
{
    private readonly ITrackingService _trackingService;
    private readonly IShipmentService _shipmentService;
    private readonly ICurrentUser _currentUser;
    private readonly IAuthorizationService _authorizationService;

    public TrackingController(
        ITrackingService trackingService,
        IShipmentService shipmentService,
        ICurrentUser currentUser,
        IAuthorizationService authorizationService)
    {
        _trackingService = trackingService;
        _shipmentService = shipmentService;
        _currentUser = currentUser;
        _authorizationService = authorizationService;
    }

    [HttpPost("location")]
    [Authorize(Roles = "DRIVER")]
    public async Task<IActionResult> RecordLocation(TrackingLocationRequest request)
    {
        var shipment = await _shipmentService.GetRawByIdAsync(request.ShipmentId);
        var authResult = await _authorizationService.AuthorizeAsync(User, shipment, "ShipmentAccessPolicy");
        if (!authResult.Succeeded)
        {
            throw new ForbiddenException("You do not have access to this shipment.");
        }

        await _trackingService.RecordLocationAsync(request, shipment, _currentUser.Id);
        return Ok();
    }

    [HttpGet("{shipmentId:guid}/live")]
    public async Task<IActionResult> GetLiveLocation(Guid shipmentId)
    {
        var shipment = await _shipmentService.GetRawByIdAsync(shipmentId);
        var authResult = await _authorizationService.AuthorizeAsync(User, shipment, "ShipmentAccessPolicy");
        if (!authResult.Succeeded)
        {
            throw new ForbiddenException("You do not have access to this shipment.");
        }

        var result = await _trackingService.GetLiveLocationAsync(shipment);
        return Ok(result);
    }

    [HttpGet("{shipmentId:guid}/history")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetHistory(Guid shipmentId)
    {
        var result = await _trackingService.GetHistoryAsync(shipmentId);
        return Ok(result);
    }
}
