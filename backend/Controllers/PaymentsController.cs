using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Logistics.Api.DTOs;
using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ICurrentUser _currentUser;

    public PaymentsController(
        IPaymentService paymentService,
        ICurrentUser currentUser)
    {
        _paymentService = paymentService;
        _currentUser = currentUser;
    }

    [HttpPost("initiate")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> Initiate(InitiatePaymentRequest request)
    {
        var response = await _paymentService.InitiatePaymentAsync(request.ShipmentId, _currentUser.Id);
        return Ok(response);
    }

    [HttpGet("{shipmentId:guid}/status")]
    public async Task<IActionResult> GetStatus(Guid shipmentId)
    {
        var response = await _paymentService.GetPaymentStatusAsync(shipmentId, _currentUser.Id, _currentUser.Role);
        return Ok(response);
    }

    [HttpPost("/api/webhooks/payment")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        string signature = Request.Headers["X-Razorpay-Signature"].ToString();
        using var reader = new StreamReader(Request.Body);
        string payload = await reader.ReadToEndAsync();

        await _paymentService.ProcessWebhookAsync(payload, signature);
        return Ok();
    }
}
