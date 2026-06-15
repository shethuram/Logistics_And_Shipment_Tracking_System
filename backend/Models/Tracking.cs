namespace Logistics.Api.Models;

public class Tracking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ShipmentId { get; set; }

    public Guid DriverId { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Shipment Shipment { get; set; } = null!;
    public Driver Driver { get; set; } = null!;
}
