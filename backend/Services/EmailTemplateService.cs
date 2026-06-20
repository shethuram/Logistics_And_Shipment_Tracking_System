using Logistics.Api.Interfaces.Services;
using Logistics.Api.Models;

namespace Logistics.Api.Services;

public class EmailTemplateService : IEmailTemplateService
{
    public (string Subject, string Body) GenerateShipmentConfirmation(Shipment shipment, string customerName, string senderOtp, string receiverOtp)
    {
        var trackingLink = $"/api/public/track?orderId={shipment.OrderId}&phone={shipment.ReceiverPhone}&date={shipment.CreatedAt:yyyy-MM-dd}";

        var subject = $"Shipment Confirmation - {shipment.OrderId}";
        var body = $@"
Dear {customerName},

Your shipment with Order ID {shipment.OrderId} has been successfully created.

--- Shipment Details ---
Pickup Address: {shipment.PickupAddress}
Drop-off Address: {shipment.DropAddress}
Receiver Name: {shipment.ReceiverName}
Receiver Phone: {shipment.ReceiverPhone}
Package Type: {shipment.PackageType}
Weight: {shipment.WeightKg} kg

--- Tracking Info ---
You can track the shipment status using the link below:
{trackingLink}

Tracking Details (to share with the receiver):
- Order ID: {shipment.OrderId}
- Phone Number: {shipment.ReceiverPhone}
- Date: {shipment.CreatedAt:yyyy-MM-dd}

--- Security OTPs ---
- **Sender OTP (for Pickup confirmation)**: {senderOtp} (Share this with the driver only at pickup)
- **Receiver OTP (for Delivery confirmation)**: {receiverOtp} (Please share this OTP and the tracking link with the receiver so they can confirm delivery with the driver)

Thank you for choosing Logistics Tracking System!
";
        return (subject, body);
    }
}
