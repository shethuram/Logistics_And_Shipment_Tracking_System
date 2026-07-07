namespace Logistics.Api.Models;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ShipmentId { get; set; }

    public PaymentMethod Method { get; set; }

    public decimal Amount { get; set; }

    public decimal DeliveryCharge { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal DriverCommission { get; set; }
    public decimal DriverEarnings { get; set; }
    public decimal Cgst { get; set; }
    public decimal Sgst { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;

    public string? IdempotencyKey { get; set; }

    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Shipment Shipment { get; set; } = null!;
}
