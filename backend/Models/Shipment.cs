namespace Logistics.Api.Models;

public class Shipment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string OrderId { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public Guid? DriverId { get; set; }

    public Guid? VehicleId { get; set; }

    public ShipmentStatus Status { get; set; } = ShipmentStatus.PENDING_PAYMENT;

    public string PickupAddress { get; set; } = string.Empty;
    public decimal PickupLat { get; set; }
    public decimal PickupLng { get; set; }

    public string DropAddress { get; set; } = string.Empty;
    public decimal DropLat { get; set; }
    public decimal DropLng { get; set; }

    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;

    public PackageType PackageType { get; set; }
    public decimal WeightKg { get; set; }
    public PreferredWindow? PreferredWindow { get; set; }
    public string? SpecialNotes { get; set; }

    public bool RiskFlag { get; set; }
    public RiskSeverity RiskSeverity { get; set; } = RiskSeverity.NONE;
    public string? RiskReason { get; set; }
    public TimeOnly? PreferredDeliveryAfter { get; set; }
    public string? DriverInstruction { get; set; }

    public string? SenderOtpHash { get; set; }
    public int SenderOtpAttempts { get; set; }
    public DateTime? SenderOtpExpiresAt { get; set; }
    public string? ReceiverOtpHash { get; set; }
    public int ReceiverOtpAttempts { get; set; }
    public DateTime? ReceiverOtpExpiresAt { get; set; }

    public bool CashCollected { get; set; }

    public Guid? StatusChangedBy { get; set; }
    public DateTime? StatusUpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Customer { get; set; } = null!;
    public Driver? Driver { get; set; }
    public Vehicle? Vehicle { get; set; }
    public Payment? Payment { get; set; }
    public ICollection<Tracking> TrackingPings { get; set; } = new List<Tracking>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();
}
