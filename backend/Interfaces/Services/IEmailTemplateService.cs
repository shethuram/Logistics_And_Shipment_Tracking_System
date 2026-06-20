using Logistics.Api.Models;

namespace Logistics.Api.Interfaces.Services;

public interface IEmailTemplateService
{
    (string Subject, string Body) GenerateShipmentConfirmation(Shipment shipment, string customerName, string senderOtp, string receiverOtp);
}
