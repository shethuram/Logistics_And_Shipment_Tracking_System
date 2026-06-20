using System;
using System.Threading.Tasks;
using Logistics.Api.DTOs;
using Logistics.Api.Models;

namespace Logistics.Api.Interfaces.Services;

public interface IAdminService
{
    Task<ReassignShipmentResponse> ReassignShipmentAsync(Guid shipmentId);
    Task<AdminMetricsResponse> GetMetricsAsync();
    Task<byte[]> ExportShipmentsCsvAsync(ShipmentStatus? status, DateTime? dateFrom, DateTime? dateTo);
}
