using System;

namespace Logistics.Api.DTOs;

public record ReassignShipmentResponse
{
    public Guid Id { get; init; }
    public string Status { get; init; } = "OPEN";
    public Guid? DriverId { get; init; } = null;
}

public record AdminMetricsResponse
{
    public int TotalShipments { get; init; }
    public int Delivered { get; init; }
    public int Pending { get; init; }
    public int Cancelled { get; init; }
    public int Failed { get; init; }
    public int AvgDeliveryTimeMinutes { get; init; }
    public int StaleShipments { get; init; }
    public int CodPending { get; init; }
    public int DriversOnline { get; init; }
    public int DriversWithHighCancelCount { get; init; }
}
