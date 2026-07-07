using Logistics.Api.Models;

namespace Logistics.Api.Interfaces.Services;

public interface IEmailTemplateService
{
    (string Subject, string Body) GenerateDriverAssignedNotification(
        Shipment shipment,
        string customerName,
        string driverName,
        string vehicleNumber,
        string vehicleType,
        string senderOtp,
        string receiverOtp);
}
