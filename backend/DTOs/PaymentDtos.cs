using System;
using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.DTOs;

public record InitiatePaymentRequest
{
    [Required]
    public Guid ShipmentId { get; init; }
}

public record InitiatePaymentResponse
{
    public string RazorpayOrderId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "INR";
}

public record PaymentStatusResponse
{
    public Guid ShipmentId { get; init; }
    public string PaymentStatus { get; init; } = string.Empty;
}
