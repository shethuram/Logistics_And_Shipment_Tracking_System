using System;
using System.Threading.Tasks;
using Logistics.Api.DTOs;
using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Logistics.Api.Exceptions;
using Logistics.Api.Models;

namespace Logistics.Api.Controllers;

[ApiController]
public class DisputesController : ControllerBase
{
    private readonly IDisputeService _disputeService;
    private readonly IShipmentService _shipmentService;
    private readonly ICurrentUser _currentUser;
    private readonly IAuthorizationService _authorizationService;

    public DisputesController(
        IDisputeService disputeService,
        IShipmentService shipmentService,
        ICurrentUser currentUser,
        IAuthorizationService authorizationService)
    {
        _disputeService = disputeService;
        _shipmentService = shipmentService;
        _currentUser = currentUser;
        _authorizationService = authorizationService;
    }

    [HttpPost("api/disputes")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> RaiseDispute(RaiseDisputeRequest request)
    {
        var shipment = await _shipmentService.GetRawByIdAsync(request.ShipmentId);
        var authResult = await _authorizationService.AuthorizeAsync(User, shipment, "ShipmentAccessPolicy");
        if (!authResult.Succeeded)
        {
            throw new ForbiddenException("You do not have access to this shipment.");
        }

        var result = await _disputeService.RaiseDisputeAsync(shipment, request.ComplaintText, _currentUser.Id);
        return CreatedAtAction(nameof(RaiseDispute), new { id = result.Id }, result);
    }

    [HttpGet("api/disputes/my")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> GetMyDisputes()
    {
        var result = await _disputeService.GetMyDisputesAsync(_currentUser.Id);
        return Ok(result);
    }

    [HttpGet("api/admin/disputes")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetDisputes(
        [FromQuery] DisputeStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _disputeService.GetDisputesAsync(status, page, pageSize);
        return Ok(result);
    }

    [HttpPost("api/admin/disputes/{id:guid}/resolve")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ResolveDispute(Guid id, ResolveDisputeRequest request)
    {
        var result = await _disputeService.ResolveDisputeAsync(id, request, _currentUser.Id);
        return Ok(result);
    }
}
