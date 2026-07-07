using System;
using System.Threading.Tasks;
using Logistics.Api.DTOs;

namespace Logistics.Api.Interfaces.Services;

public interface IPaymentService
{
    Task<InitiatePaymentResponse> InitiatePaymentAsync(Guid shipmentId, Guid customerUserId);
    Task<PaymentStatusResponse> GetPaymentStatusAsync(Guid shipmentId, Guid userId, string userRole);
    Task ProcessWebhookAsync(string payload, string signature);
}
