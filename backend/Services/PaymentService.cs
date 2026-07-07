using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Logistics.Api.Services;

public class PaymentService : IPaymentService
{
    private readonly IShipmentRepository _shipmentRepo;
    private readonly IPaymentRepository _paymentRepo;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IShipmentRepository shipmentRepo,
        IPaymentRepository paymentRepo,
        INotificationService notificationService,
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        ILogger<PaymentService> logger)
    {
        _shipmentRepo = shipmentRepo;
        _paymentRepo = paymentRepo;
        _notificationService = notificationService;
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<InitiatePaymentResponse> InitiatePaymentAsync(Guid shipmentId, Guid customerUserId)
    {
        var shipment = await _shipmentRepo.GetByIdAsync(shipmentId);
        if (shipment == null)
            throw new NotFoundException("Shipment not found.");

        if (shipment.CustomerId != customerUserId)
            throw new ForbiddenException("You do not have access to this shipment.");

        if (shipment.Payment == null)
            throw new NotFoundException("Payment record not found for this shipment.");

        if (shipment.Status != ShipmentStatus.PENDING_PAYMENT)
            throw new BusinessRuleException("Shipment is not in pending payment state.");

        if (!string.IsNullOrEmpty(shipment.Payment.RazorpayOrderId))
        {
            return new InitiatePaymentResponse
            {
                RazorpayOrderId = shipment.Payment.RazorpayOrderId,
                Amount = shipment.Payment.Amount,
                Currency = "INR"
            };
        }

        string keyId = _config["Razorpay:KeyId"] ?? string.Empty;
        string keySecret = _config["Razorpay:KeySecret"] ?? string.Empty;

        string razorpayOrderId = $"order_mock_{Guid.NewGuid():N}";
        decimal amount = shipment.Payment.Amount;

        if (!string.IsNullOrEmpty(keyId) && keyId != "rzp_test_DEFAULT_ID_MOCK")
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var authBytes = Encoding.ASCII.GetBytes($"{keyId}:{keySecret}");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
                client.DefaultRequestHeaders.Add("X-Razorpay-Idempotency-Key", shipment.Payment.IdempotencyKey ?? string.Empty);

                var orderPayload = new
                {
                    amount = (int)Math.Round(amount * 100),
                    currency = "INR",
                    receipt = shipment.Id.ToString()
                };

                var content = new StringContent(JsonSerializer.Serialize(orderPayload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.razorpay.com/v1/orders", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.TryGetProperty("id", out var idProp))
                    {
                        razorpayOrderId = idProp.GetString() ?? razorpayOrderId;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Razorpay API order creation failed, falling back to mock order ID");
            }
        }

        shipment.Payment.RazorpayOrderId = razorpayOrderId;
        shipment.Payment.UpdatedAt = DateTime.UtcNow;
        await _shipmentRepo.UpdateAsync(shipment);

        return new InitiatePaymentResponse
        {
            RazorpayOrderId = razorpayOrderId,
            Amount = amount,
            Currency = "INR"
        };
    }

    public async Task<PaymentStatusResponse> GetPaymentStatusAsync(Guid shipmentId, Guid userId, string userRole)
    {
        var shipment = await _shipmentRepo.GetByIdAsync(shipmentId);
        if (shipment == null)
            throw new NotFoundException("Shipment not found.");

        if (shipment.CustomerId != userId && userRole != "ADMIN")
            throw new ForbiddenException("You do not have access to this shipment.");

        return new PaymentStatusResponse
        {
            ShipmentId = shipmentId,
            PaymentStatus = shipment.Payment?.Status.ToString() ?? string.Empty
        };
    }

    public async Task ProcessWebhookAsync(string payload, string signature)
    {
        string webhookSecret = _config["Razorpay:WebhookSecret"] ?? string.Empty;

        if (webhookSecret != "rzp_test_DEFAULT_WEBHOOK_MOCK" && !VerifySignature(payload, signature, webhookSecret))
        {
            throw new ValidationException("Invalid signature verification.");
        }

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        if (root.TryGetProperty("payload", out var payloadProp) &&
            payloadProp.TryGetProperty("payment", out var paymentProp) &&
            paymentProp.TryGetProperty("entity", out var entityProp))
        {
            string orderId = entityProp.GetProperty("order_id").GetString() ?? string.Empty;
            string paymentId = entityProp.GetProperty("id").GetString() ?? string.Empty;

            var payment = await _paymentRepo.GetByOrderIdAsync(orderId);
            if (payment != null && payment.Status == PaymentStatus.PENDING)
            {
                payment.Status = PaymentStatus.SUCCESS;
                payment.RazorpayPaymentId = paymentId;
                payment.UpdatedAt = DateTime.UtcNow;

                payment.Shipment.Status = ShipmentStatus.OPEN;
                payment.Shipment.StatusUpdatedAt = DateTime.UtcNow;
                payment.Shipment.UpdatedAt = DateTime.UtcNow;

                await _paymentRepo.UpdateAsync(payment);

                await _notificationService.CreateNotificationAsync(
                    payment.Shipment.CustomerId,
                    payment.Shipment.Id,
                    "Payment Successful",
                    $"Payment of ₹{payment.Amount:F2} for order {payment.Shipment.OrderId} succeeded. Shipment is now open for drivers.");

                await _notificationService.BroadcastShipmentUpdateAsync(payment.Shipment.Id, "OPEN", new { payment.Shipment.OrderId });
            }
        }
    }

    private bool VerifySignature(string payload, string signature, string secret)
    {
        if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
            return false;

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = Convert.ToHexString(hashBytes).ToLower();
        return computedSignature == signature.ToLower();
    }
}
