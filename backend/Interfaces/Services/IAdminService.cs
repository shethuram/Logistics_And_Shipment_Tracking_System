using System;
using System.Threading.Tasks;
using Logistics.Api.DTOs;

namespace Logistics.Api.Interfaces.Services;

public interface IAdminService
{
    Task<ReassignShipmentResponse> ReassignShipmentAsync(Guid shipmentId);
    Task<AdminMetricsResponse> GetMetricsAsync();
    Task<byte[]> ExportShipmentsCsvAsync(string? status, DateTime? dateFrom, DateTime? dateTo);
}
