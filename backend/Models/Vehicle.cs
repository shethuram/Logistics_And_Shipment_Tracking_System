namespace Logistics.Api.Models;

public class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DriverId { get; set; }

    public VehicleType VehicleType { get; set; }

    public string VehicleNumber { get; set; } = string.Empty;

    public bool IsActive { get; set; } = false ; 

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Driver Driver { get; set; } = null!;
}
