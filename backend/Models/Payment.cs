namespace Logistics.Api.Models;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ShipmentId { get; set; }

    public PaymentMethod Method { get; set; }

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;

    public string? IdempotencyKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Shipment Shipment { get; set; } = null!;
}
