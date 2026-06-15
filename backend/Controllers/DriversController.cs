using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/drivers/{id:guid}")]
[Authorize(Roles = "DRIVER")]
public class DriversController : ControllerBase
{
    private readonly IDriverService _driverService;
    private readonly ICurrentUser _currentUser;

    public DriversController(IDriverService driverService, ICurrentUser currentUser)
    {
        _driverService = driverService;
        _currentUser = currentUser;
    }

    [HttpPost("go-online")]
    public async Task<IActionResult> GoOnline(Guid id, GoOnlineRequest request)
    {
        if (id != _currentUser.Id)
            throw new ForbiddenException("You are not authorized to modify this driver's profile.");

        var result = await _driverService.GoOnlineAsync(id, request);
        return Ok(result);
    }

    [HttpPost("go-offline")]
    public async Task<IActionResult> GoOffline(Guid id)
    {
        if (id != _currentUser.Id)
            throw new ForbiddenException("You are not authorized to modify this driver's profile.");

        var result = await _driverService.GoOfflineAsync(id);
        return Ok(result);
    }
}
