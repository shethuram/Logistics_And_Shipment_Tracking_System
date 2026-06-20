using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Logistics.Api.Interfaces.Repositories;
namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/drivers/vehicles")]
[Authorize(Roles = "DRIVER")]
public class DriverVehiclesController : ControllerBase
{
    private readonly IDriverVehicleService _vehicleService;
    private readonly IDriverRepository _driverRepo;
    private readonly ICurrentUser _currentUser;

    public DriverVehiclesController(IDriverVehicleService vehicleService, IDriverRepository driverRepo, ICurrentUser currentUser)
    {
        _vehicleService = vehicleService;
        _driverRepo = driverRepo;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetVehicles()
    {
        var driver = await _driverRepo.GetByUserIdAsync(_currentUser.Id);
        if (driver == null) throw new NotFoundException("Driver profile not found.");

        var result = await _vehicleService.GetVehiclesAsync(driver.Id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> AddVehicle(AddVehicleRequest request)
    {
        var driver = await _driverRepo.GetByUserIdAsync(_currentUser.Id);
        if (driver == null) throw new NotFoundException("Driver profile not found.");

        var result = await _vehicleService.AddVehicleAsync(driver.Id, request);
        return CreatedAtAction(nameof(GetVehicles), new { }, result);
    }

    [HttpPut("{vehicleId:guid}")]
    public async Task<IActionResult> UpdateVehicle(Guid vehicleId, UpdateVehicleRequest request)
    {
        var driver = await _driverRepo.GetByUserIdAsync(_currentUser.Id);
        if (driver == null) throw new NotFoundException("Driver profile not found.");

        var result = await _vehicleService.UpdateVehicleAsync(driver.Id, vehicleId, request);
        return Ok(result);
    }

    [HttpPost("{vehicleId:guid}/set-active")]
    public async Task<IActionResult> SetActive(Guid vehicleId)
    {
        var driver = await _driverRepo.GetByUserIdAsync(_currentUser.Id);
        if (driver == null) throw new NotFoundException("Driver profile not found.");

        var result = await _vehicleService.SetActiveVehicleAsync(driver.Id, vehicleId);
        return Ok(result);
    }
}
