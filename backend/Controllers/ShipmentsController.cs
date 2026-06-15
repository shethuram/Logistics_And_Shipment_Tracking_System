using Logistics.Api.DTOs;
using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/shipments")]
[Authorize]
public class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _shipmentService;
    private readonly ICurrentUser _currentUser;

    public ShipmentsController(IShipmentService shipmentService, ICurrentUser currentUser)
    {
        _shipmentService = shipmentService;
        _currentUser = currentUser;
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
        var result = await _shipmentService.GetByIdAsync(id, _currentUser.Id, _currentUser.Role);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> Update(Guid id, UpdateShipmentRequest request)
    {
        await _shipmentService.UpdateAsync(id, request, _currentUser.Id);
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await _shipmentService.CancelAsync(id, _currentUser.Id);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetShipments(
        [FromQuery] string? search,
        [FromQuery] string? status,
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
        var result = await _shipmentService.CancelClaimAsync(id, _currentUser.Id, request);
        return Ok(result);
    }

    [HttpPost("{id:guid}/confirm-pickup")]
    [Authorize(Roles = "DRIVER")]
    public async Task<IActionResult> ConfirmPickup(Guid id, ConfirmPickupRequest request)
    {
        var result = await _shipmentService.ConfirmPickupAsync(id, _currentUser.Id, request);
        return Ok(result);
    }

    [HttpPost("{id:guid}/confirm-delivery")]
    [Authorize(Roles = "DRIVER")]
    public async Task<IActionResult> ConfirmDelivery(Guid id, ConfirmDeliveryRequest request)
    {
        var result = await _shipmentService.ConfirmDeliveryAsync(id, _currentUser.Id, request);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cash-collected")]
    [Authorize(Roles = "DRIVER")]
    public async Task<IActionResult> ConfirmCashCollected(Guid id)
    {
        var result = await _shipmentService.ConfirmCashCollectedAsync(id, _currentUser.Id);
        return Ok(result);
    }

    [HttpPost("{id:guid}/pickup-failed")]
    [Authorize(Roles = "DRIVER")]
    public async Task<IActionResult> MarkPickupFailed(Guid id, PickupFailedRequest request)
    {
        var result = await _shipmentService.MarkPickupFailedAsync(id, _currentUser.Id, request);
        return Ok(result);
    }
}
