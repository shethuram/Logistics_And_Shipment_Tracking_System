using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
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
    private readonly ICurrentUser _currentUser;

    public DriverVehiclesController(IDriverVehicleService vehicleService, ICurrentUser currentUser)
    {
        _vehicleService = vehicleService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetVehicles(Guid id)
    {
        if (id != _currentUser.Id)
            throw new ForbiddenException("You are not authorized to view this driver's profile.");

        var result = await _vehicleService.GetVehiclesAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> AddVehicle(Guid id, AddVehicleRequest request)
    {
        if (id != _currentUser.Id)
            throw new ForbiddenException("You are not authorized to modify this driver's profile.");

        var result = await _vehicleService.AddVehicleAsync(id, request);
        return CreatedAtAction(nameof(GetVehicles), new { id }, result);
    }

    [HttpPut("{vehicleId:guid}")]
    public async Task<IActionResult> UpdateVehicle(Guid id, Guid vehicleId, UpdateVehicleRequest request)
    {
        if (id != _currentUser.Id)
            throw new ForbiddenException("You are not authorized to modify this driver's profile.");

        var result = await _vehicleService.UpdateVehicleAsync(id, vehicleId, request);
        return Ok(result);
    }

    [HttpPost("{vehicleId:guid}/set-active")]
    public async Task<IActionResult> SetActive(Guid id, Guid vehicleId)
    {
        if (id != _currentUser.Id)
            throw new ForbiddenException("You are not authorized to modify this driver's profile.");

        var result = await _vehicleService.SetActiveVehicleAsync(id, vehicleId);
        return Ok(result);
    }
}
