using System;
using System.Threading.Tasks;
using Logistics.Api.DTOs;
using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "ADMIN")]
public class AdminShipmentsController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IShipmentService _shipmentService;
    private readonly ICurrentUser _currentUser;

    public AdminShipmentsController(
        IAdminService adminService,
        IShipmentService shipmentService,
        ICurrentUser currentUser)
    {
        _adminService = adminService;
        _shipmentService = shipmentService;
        _currentUser = currentUser;
    }

    [HttpGet("shipments")]
    public async Task<IActionResult> GetAdminShipments(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _shipmentService.GetShipmentsAsync(
            _currentUser.Id, "ADMIN", search, status, dateFrom, dateTo, page, pageSize);

        return Ok(result);
    }

    [HttpPost("shipments/{id:guid}/reassign")]
    public async Task<IActionResult> ReassignShipment(Guid id)
    {
        var result = await _adminService.ReassignShipmentAsync(id);
        return Ok(result);
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        var result = await _adminService.GetMetricsAsync();
        return Ok(result);
    }

    [HttpGet("export/shipments")]
    public async Task<IActionResult> ExportShipments(
        [FromQuery] string? status,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo)
    {
        var csvBytes = await _adminService.ExportShipmentsCsvAsync(status, dateFrom, dateTo);
        return File(csvBytes, "text/csv", "shipments_export.csv");
    }
}
