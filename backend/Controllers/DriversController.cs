using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/drivers")]
[Authorize(Roles = "DRIVER,DRIVER_PENDING,DRIVER_REJECTED")]
public class DriversController : ControllerBase
{
    private readonly IDriverService _driverService;
    private readonly IDriverRepository _driverRepo;
    private readonly ICurrentUser _currentUser;

    public DriversController(IDriverService driverService, IDriverRepository driverRepo, ICurrentUser currentUser)
    {
        _driverService = driverService;
        _driverRepo = driverRepo;
        _currentUser = currentUser;
    }

    [HttpPost("go-online")]
    [Authorize(Roles = "DRIVER")]
    public async Task<IActionResult> GoOnline(GoOnlineRequest request)
    {
        var driver = await _driverRepo.GetByUserIdAsync(_currentUser.Id);
        if (driver == null) throw new NotFoundException("Driver profile not found.");

        var result = await _driverService.GoOnlineAsync(driver.Id, request);
        return Ok(result);
    }

    [HttpPost("go-offline")]
    [Authorize(Roles = "DRIVER")]
    public async Task<IActionResult> GoOffline()
    {
        var driver = await _driverRepo.GetByUserIdAsync(_currentUser.Id);
        if (driver == null) throw new NotFoundException("Driver profile not found.");

        var result = await _driverService.GoOfflineAsync(driver.Id);
        return Ok(result);
    }

    [HttpGet("me/allowed-vehicles")]
    public async Task<IActionResult> GetAllowedVehicles()
    {
        var driver = await _driverRepo.GetByUserIdAsync(_currentUser.Id);
        if (driver == null) throw new NotFoundException("Driver profile not found.");

        var allowed = driver.AllowedVehicleTypes ?? Enum.GetNames<VehicleType>();
        return Ok(allowed);
    }

    [HttpPut("profile")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateProfile([FromForm] UpdateDriverProfileRequest request, Microsoft.AspNetCore.Http.IFormFile? licenseFile)
    {
        var driver = await _driverRepo.GetByUserIdAsync(_currentUser.Id);
        if (driver == null) throw new NotFoundException("Driver profile not found.");

        await _driverService.UpdateDriverProfileAsync(driver.Id, request, licenseFile);
        return Ok(new { message = "Profile updated successfully, verification triggered." });
    }
}
