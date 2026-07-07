using Logistics.Api.Interfaces.Services;
using Logistics.Api.Models;
using System;

namespace Logistics.Api.Services;

public class EmailTemplateService : IEmailTemplateService
{
    public (string Subject, string Body) GenerateDriverAssignedNotification(
        Shipment shipment,
        string customerName,
        string driverName,
        string vehicleNumber,
        string vehicleType,
        string senderOtp,
        string receiverOtp)
    {
        var trackingLink = $"/api/public/track?orderId={shipment.OrderId}&phone={shipment.ReceiverPhone}&date={shipment.CreatedAt:yyyy-MM-dd}";
        
        var deliveryCharge = shipment.Payment != null ? Math.Round(shipment.Payment.Amount / 1.18m, 2) : 0m;
        var cgst = shipment.Payment != null ? Math.Round(deliveryCharge * 0.09m, 2) : 0m;
        var sgst = shipment.Payment != null ? Math.Round(deliveryCharge * 0.09m, 2) : 0m;
        var totalAmount = shipment.Payment?.Amount ?? 0m;

        var subject = $"Driver Assigned for Shipment {shipment.OrderId}";
        var body = $@"
Dear {customerName},

A driver has claimed your shipment and is heading to the pickup address!

--- Driver Details ---
Name: {driverName}
Vehicle: {vehicleType} ({vehicleNumber})

--- Security OTPs ---
- **Sender OTP (for Pickup)**: {senderOtp} (Share with the driver only at pickup)
- **Receiver OTP (for Delivery)**: {receiverOtp} (Share this OTP with the receiver)

--- Invoice Details ---
Delivery Charge: ₹{deliveryCharge:F2}
CGST (9%): ₹{cgst:F2}
SGST (9%): ₹{sgst:F2}
----------------------
Total Paid: ₹{totalAmount:F2} (via {shipment.Payment?.Method})

You can track your driver's live location using this link:
{trackingLink}

Thank you for choosing Logistics Tracking System!
";
        return (subject, body);
    }
}
