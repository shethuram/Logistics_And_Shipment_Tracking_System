using Logistics.Api.DTOs;
using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/drivers/{id:guid}/vehicles")]
[Authorize(Roles = "DRIVER")]
public class DriverVehiclesController : ControllerBase
{
    private readonly IDriverVehicleService _vehicleService;

    public DriverVehiclesController(IDriverVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetVehicles(Guid id)
    {
        var result = await _vehicleService.GetVehiclesAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> AddVehicle(Guid id, AddVehicleRequest request)
    {
        var result = await _vehicleService.AddVehicleAsync(id, request);
        return CreatedAtAction(nameof(GetVehicles), new { id }, result);
    }

    [HttpPut("{vehicleId:guid}")]
    public async Task<IActionResult> UpdateVehicle(Guid id, Guid vehicleId, UpdateVehicleRequest request)
    {
        var result = await _vehicleService.UpdateVehicleAsync(id, vehicleId, request);
        return Ok(result);
    }

    [HttpPost("{vehicleId:guid}/set-active")]
    public async Task<IActionResult> SetActive(Guid id, Guid vehicleId)
    {
        var result = await _vehicleService.SetActiveVehicleAsync(id, vehicleId);
        return Ok(result);
    }
}
