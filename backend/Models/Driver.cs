namespace Logistics.Api.Models;

public class Driver
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string LicenseNumber { get; set; } = string.Empty;

    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.PENDING;

    public OperationalStatus OperationalStatus { get; set; } = OperationalStatus.OFFLINE;

    public Guid? ActiveVehicleId { get; set; }

    public decimal? CurrentLat { get; set; }

    public decimal? CurrentLng { get; set; }

    public DateTime? LastPingAt { get; set; }

    public string? ApprovalReason { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public int CancelCount { get; set; }

    public string? LicenseFileUrl { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.NOT_STARTED;
    public string? VerificationReport { get; set; }
    public string[]? LicenseClasses { get; set; }
    public string[]? AllowedVehicleTypes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Vehicle? ActiveVehicle { get; set; }
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    public ICollection<Tracking> TrackingPings { get; set; } = new List<Tracking>();
}
