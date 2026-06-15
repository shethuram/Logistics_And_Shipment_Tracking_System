using Logistics.Api.DTOs;
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

    public DriversController(IDriverService driverService)
    {
        _driverService = driverService;
    }

    [HttpPost("go-online")]
    public async Task<IActionResult> GoOnline(Guid id, GoOnlineRequest request)
    {
        var result = await _driverService.GoOnlineAsync(id, request);
        return Ok(result);
    }

    [HttpPost("go-offline")]
    public async Task<IActionResult> GoOffline(Guid id)
    {
        var result = await _driverService.GoOfflineAsync(id);
        return Ok(result);
    }
}
