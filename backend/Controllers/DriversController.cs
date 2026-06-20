using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/drivers")]
[Authorize(Roles = "DRIVER")]
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
    public async Task<IActionResult> GoOnline(GoOnlineRequest request)
    {
        var driver = await _driverRepo.GetByUserIdAsync(_currentUser.Id);
        if (driver == null) throw new NotFoundException("Driver profile not found.");

        var result = await _driverService.GoOnlineAsync(driver.Id, request);
        return Ok(result);
    }

    [HttpPost("go-offline")]
    public async Task<IActionResult> GoOffline()
    {
        var driver = await _driverRepo.GetByUserIdAsync(_currentUser.Id);
        if (driver == null) throw new NotFoundException("Driver profile not found.");

        var result = await _driverService.GoOfflineAsync(driver.Id);
        return Ok(result);
    }
}
