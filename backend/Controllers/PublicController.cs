using Logistics.Api.DTOs;
using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly IShipmentService _shipmentService;

    public PublicController(IShipmentService shipmentService)
    {
        _shipmentService = shipmentService;
    }

    [HttpGet("track")]
    public async Task<IActionResult> Track(
        [FromQuery] string? orderId,
        [FromQuery] string? phone,
        [FromQuery] string? date)
    {
        var result = await _shipmentService.GetPublicTrackingAsync(orderId ?? string.Empty, phone ?? string.Empty, date ?? string.Empty);
        return Ok(result);
    }
}
