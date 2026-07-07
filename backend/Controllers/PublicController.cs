using Logistics.Api.DTOs;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly IShipmentService _shipmentService;

    public PublicController(IShipmentService shipmentService)
    {
        _shipmentService = shipmentService;
    }

    [HttpGet("track")]
    public async Task<IActionResult> Track(
        [FromQuery] string? orderId,
        [FromQuery] string? phone,
        [FromQuery] string? date)
    {
        var result = await _shipmentService.GetPublicTrackingAsync(orderId ?? string.Empty, phone ?? string.Empty, date ?? string.Empty);
        return Ok(result);
    }

    [HttpGet("metadata")]
    public IActionResult GetMetadata()
    {
        var packageTypes = Enum.GetValues<PackageType>()
            .Select(e => new { Value = e.ToString(), Label = GetFriendlyPackageTypeLabel(e) });

        var preferredWindows = Enum.GetValues<PreferredWindow>()
            .Select(e => new { Value = e.ToString(), Label = GetFriendlyPreferredWindowLabel(e) });

        var paymentMethods = Enum.GetValues<PaymentMethod>()
            .Select(e => new { Value = e.ToString(), Label = GetFriendlyPaymentMethodLabel(e) });

        var vehicleTypes = Enum.GetValues<VehicleType>()
            .Select(e => new { Value = e.ToString(), Label = GetFriendlyVehicleTypeLabel(e) });

        var shipmentStatuses = Enum.GetValues<ShipmentStatus>()
            .Select(e => new { Value = e.ToString(), Label = GetFriendlyShipmentStatusLabel(e) });

        return Ok(new
        {
            PackageTypes = packageTypes,
            PreferredWindows = preferredWindows,
            PaymentMethods = paymentMethods,
            VehicleTypes = vehicleTypes,
            ShipmentStatuses = shipmentStatuses
        });
    }

    private static string GetFriendlyPackageTypeLabel(PackageType type) => type switch
    {
        PackageType.DOCUMENT => "Document",
        PackageType.SMALL_PARCEL => "Small Parcel",
        PackageType.LARGE_PARCEL => "Large Parcel",
        PackageType.FRAGILE => "Fragile Items",
        PackageType.HOUSEHOLD => "Household Goods",
        _ => type.ToString()
    };

    private static string GetFriendlyPreferredWindowLabel(PreferredWindow window) => window switch
    {
        PreferredWindow.MORNING => "Morning (8 AM - 12 PM)",
        PreferredWindow.AFTERNOON => "Afternoon (12 PM - 4 PM)",
        PreferredWindow.EVENING => "Evening (4 PM - 8 PM)",
        _ => window.ToString()
    };

    private static string GetFriendlyPaymentMethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.COD => "Cash on Delivery (COD)",
        PaymentMethod.ONLINE => "Online Payment (Razorpay)",
        _ => method.ToString()
    };

    private static string GetFriendlyVehicleTypeLabel(VehicleType type) => type switch
    {
        VehicleType.TWO_WHEELER => "Two Wheeler",
        VehicleType.THREE_WHEELER => "Three Wheeler",
        VehicleType.FOUR_WHEELER => "Four Wheeler",
        VehicleType.HEAVY_VEHICLE => "Heavy Vehicle",
        _ => type.ToString()
    };

    private static string GetFriendlyShipmentStatusLabel(ShipmentStatus status) => status switch
    {
        ShipmentStatus.PENDING_PAYMENT => "Pending Payment",
        ShipmentStatus.OPEN => "Open / Unassigned",
        ShipmentStatus.ASSIGNED => "Driver Assigned",
        ShipmentStatus.IN_TRANSIT => "In Transit",
        ShipmentStatus.DELIVERED => "Delivered",
        ShipmentStatus.CANCELLED => "Cancelled",
        ShipmentStatus.PICKUP_FAILED => "Pickup Failed",
        ShipmentStatus.STALE => "Stale",
        _ => status.ToString()
    };
}
