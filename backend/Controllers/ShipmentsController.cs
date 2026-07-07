using Logistics.Api.DTOs;
using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Logistics.Api.Exceptions;
using Logistics.Api.Models;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/shipments")]
[Authorize]
public class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _shipmentService;
    private readonly ICurrentUser _currentUser;
    private readonly IAuthorizationService _authorizationService;

    public ShipmentsController(
        IShipmentService shipmentService,
        ICurrentUser currentUser,
        IAuthorizationService authorizationService)
    {
        _shipmentService = shipmentService;
        _currentUser = currentUser;
        _authorizationService = authorizationService;
    }

    [HttpPost]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> Create(CreateShipmentRequest request)
    {
        var result = await _shipmentService.CreateAsync(request, _currentUser.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var shipment = await _shipmentService.GetRawByIdAsync(id);
        var authResult = await _authorizationService.AuthorizeAsync(User, shipment, "ShipmentAccessPolicy");
        if (!authResult.Succeeded)
        {
            throw new ForbiddenException("You do not have access to this shipment.");
        }

        var result = await _shipmentService.GetByIdAsync(shipment);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> Update(Guid id, UpdateShipmentRequest request)
    {
        var shipment = await _shipmentService.GetRawByIdAsync(id);
        var authResult = await _authorizationService.AuthorizeAsync(User, shipment, "ShipmentAccessPolicy");
        if (!authResult.Succeeded)
        {
            throw new ForbiddenException("You do not have access to this shipment.");
        }

        await _shipmentService.UpdateAsync(shipment, request);
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var shipment = await _shipmentService.GetRawByIdAsync(id);
        var authResult = await _authorizationService.AuthorizeAsync(User, shipment, "ShipmentAccessPolicy");
        if (!authResult.Succeeded)
        {
            throw new ForbiddenException("You do not have access to this shipment.");
        }

        var result = await _shipmentService.CancelAsync(shipment, _currentUser.Id);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "CUSTOMER,ADMIN")]
    public async Task<IActionResult> GetShipments(
        [FromQuery] string? search,
        [FromQuery] ShipmentStatus? status,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _shipmentService.GetShipmentsAsync(
            _currentUser.Id, _currentUser.Role, search, status, dateFrom, dateTo, page, pageSize);
        return Ok(result);
    }

    [HttpGet("available")]
    [Authorize(Roles = "DRIVER")]
    public async Task<IActionResult> GetAvailable()
    {
        var result = await _shipmentService.GetAvailableShipmentsAsync(_currentUser.Id);
        return Ok(result);
    }

    [HttpGet("active")]
    [Authorize(Roles = "DRIVER")]
    public async Task<IActionResult> GetActive()
    {
        var result = await _shipmentService.GetActiveShipmentAsync(_currentUser.Id);
        if (result == null)
        {
            return NotFound(new { message = "No active shipment found." });
        }
        return Ok(result);
    }

    [HttpPost("{id:guid}/claim")]
    [Authorize(Roles = "DRIVER")]
    public async Task<IActionResult> Claim(Guid id)
    {
        var result = await _shipmentService.ClaimAsync(id, _currentUser.Id);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel-claim")]
    [Authorize(Roles = "DRIVER")]
    public async Task<IActionResult> CancelClaim(Guid id, CancelClaimRequest request)
    {
        var shipment = await _shipmentService.GetRawByIdAsync(id);
        var authResult = await _authorizationService.AuthorizeAsync(User, shipment, "ShipmentAccessPolicy");
        if (!authResult.Succeeded)
        {
            throw new ForbiddenException("You do not have access to this shipment.");
        }

        var result = await _shipmentService.CancelClaimAsync(shipment, _currentUser.Id, request);
        return Ok(result);
    }

    [HttpPost("{id:guid}/confirm-pickup")]
    [Authorize(Roles = "DRIVER")]
    public async Task<IActionResult> ConfirmPickup(Guid id, ConfirmPickupRequest request)
    {
        var shipment = await _shipmentService.GetRawByIdAsync(id);
        var authResult = await _authorizationService.AuthorizeAsync(User, shipment, "ShipmentAccessPolicy");
        if (!authResult.Succeeded)
        {
            throw new ForbiddenException("You do not have access to this shipment.");
        }

        var result = await _shipmentService.ConfirmPickupAsync(shipment, _currentUser.Id, request);
        return Ok(result);
    }

    [HttpPost("{id:guid}/confirm-delivery")]
    [Authorize(Roles = "DRIVER")]
    public async Task<IActionResult> ConfirmDelivery(Guid id, ConfirmDeliveryRequest request)
    {
        var shipment = await _shipmentService.GetRawByIdAsync(id);
        var authResult = await _authorizationService.AuthorizeAsync(User, shipment, "ShipmentAccessPolicy");
        if (!authResult.Succeeded)
        {
            throw new ForbiddenException("You do not have access to this shipment.");
        }

        var result = await _shipmentService.ConfirmDeliveryAsync(shipment, _currentUser.Id, request);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cash-collected")]
    [Authorize(Roles = "DRIVER")]
    public async Task<IActionResult> ConfirmCashCollected(Guid id)
    {
        var shipment = await _shipmentService.GetRawByIdAsync(id);
        var authResult = await _authorizationService.AuthorizeAsync(User, shipment, "ShipmentAccessPolicy");
        if (!authResult.Succeeded)
        {
            throw new ForbiddenException("You do not have access to this shipment.");
        }

        var result = await _shipmentService.ConfirmCashCollectedAsync(shipment, _currentUser.Id);
        return Ok(result);
    }

    [HttpPost("{id:guid}/pickup-failed")]
    [Authorize(Roles = "DRIVER")]
    public async Task<IActionResult> MarkPickupFailed(Guid id, PickupFailedRequest request)
    {
        var shipment = await _shipmentService.GetRawByIdAsync(id);
        var authResult = await _authorizationService.AuthorizeAsync(User, shipment, "ShipmentAccessPolicy");
        if (!authResult.Succeeded)
        {
            throw new ForbiddenException("You do not have access to this shipment.");
        }

        var result = await _shipmentService.MarkPickupFailedAsync(shipment, _currentUser.Id, request);
        return Ok(result);
    }
}
