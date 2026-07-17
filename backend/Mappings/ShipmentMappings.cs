using Logistics.Api.DTOs;
using Logistics.Api.Models;

namespace Logistics.Api.Mappings;

public static class ShipmentMappings
{
    public static CreateShipmentResponse ToCreateShipmentResponse(this Shipment s, string? paymentUrl = null) => new()
    {
        Id = s.Id,
        OrderId = s.OrderId,
        Status = s.Status,
        PaymentUrl = paymentUrl
    };

    public static ShipmentResponse ToShipmentResponse(this Shipment s) => new()
    {
        Id = s.Id,
        OrderId = s.OrderId,
        Status = s.Status,
        PickupAddress = s.PickupAddress,
        PickupLat = s.PickupLat,
        PickupLng = s.PickupLng,
        DropAddress = s.DropAddress,
        DropLat = s.DropLat,
        DropLng = s.DropLng,
        ReceiverName = s.ReceiverName,
        ReceiverPhone = s.ReceiverPhone,
        PackageType = s.PackageType,
        WeightKg = s.WeightKg,
        PreferredWindow = s.PreferredWindow,
        SpecialNotes = s.SpecialNotes,
        DriverInstruction = s.DriverInstruction,
        RiskFlag = s.RiskFlag,
        RiskSeverity = s.RiskSeverity,
        RiskReason = s.RiskReason,
        PreferredDeliveryAfter = s.PreferredDeliveryAfter,
        CashCollected = s.CashCollected,
        StatusChangedBy = s.StatusChangedBy,
        StatusUpdatedAt = s.StatusUpdatedAt,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt,
        Driver = s.Driver != null ? new ShipmentDriverDto
        {
            Id = s.Driver.Id,
            FullName = s.Driver.User?.FullName ?? string.Empty,
            VehicleType = s.Driver.ActiveVehicle?.VehicleType,
            VehicleNumber = s.Driver.ActiveVehicle?.VehicleNumber ?? string.Empty
        } : null,
        DeliveryCharge = s.Payment != null ? s.Payment.DeliveryCharge : 0m,
        PlatformFee = s.Payment != null ? s.Payment.PlatformFee : 0m,
        Cgst = s.Payment != null ? s.Payment.Cgst : 0m,
        Sgst = s.Payment != null ? s.Payment.Sgst : 0m,
        TotalAmount = s.Payment != null ? s.Payment.Amount : 0m,
        DriverEarnings = s.Payment != null ? s.Payment.DriverEarnings : 0m,
        PaymentMethod = s.Payment != null ? s.Payment.Method.ToString() : string.Empty,
        PaymentStatus = s.Payment != null ? s.Payment.Status.ToString() : string.Empty
    };

    public static AvailableShipmentDto ToAvailableShipmentDto(this Shipment s, double distanceToPickupKm) => new()
    {
        Id = s.Id,
        OrderId = s.OrderId,
        PickupAddress = s.PickupAddress,
        DropAddress = s.DropAddress,
        PackageType = s.PackageType,
        WeightKg = s.WeightKg,
        PreferredWindow = s.PreferredWindow,
        SenderPhone = s.Customer?.Phone ?? string.Empty,
        ReceiverPhone = s.ReceiverPhone,
        DistanceToPickupKm = distanceToPickupKm,
        DriverInstruction = s.DriverInstruction,
        DriverEarnings = s.Payment != null ? s.Payment.DriverEarnings : 0m,
        RiskFlag = s.RiskFlag,
        RiskSeverity = s.RiskSeverity,
        RiskReason = s.RiskReason
    };

    public static ClaimShipmentResponse ToClaimShipmentResponse(this Shipment s) => new()
    {
        Id = s.Id,
        Status = s.Status,
        OrderId = s.OrderId
    };

    public static CancelClaimResponse ToCancelClaimResponse(this Shipment s, int driverCancelCount) => new()
    {
        Id = s.Id,
        Status = s.Status,
        DriverCancelCount = driverCancelCount
    };

    public static ConfirmPickupResponse ToConfirmPickupResponse(this Shipment s) => new()
    {
        Id = s.Id,
        Status = s.Status
    };

    public static ConfirmDeliveryResponse ToConfirmDeliveryResponse(this Shipment s) => new()
    {
        Id = s.Id,
        Status = s.Status,
        DeliveredAt = s.StatusUpdatedAt ?? DateTime.UtcNow
    };

    public static CashCollectedResponse ToCashCollectedResponse(this Shipment s) => new()
    {
        Id = s.Id,
        CashCollected = s.CashCollected
    };

    public static PickupFailedResponse ToPickupFailedResponse(this Shipment s) => new()
    {
        Id = s.Id,
        Status = s.Status
    };

    public static PublicTrackingResponse ToPublicTrackingResponse(
        this Shipment s, 
        PublicTrackingDriverDto? driver, 
        DriverLocationDto? driverLocation, 
        List<TimelineEntryDto> timeline) => new()
    {
        Id = s.Id,
        OrderId = s.OrderId,
        Status = s.Status,
        PickupAddress = s.PickupAddress,
        DropAddress = s.DropAddress,
        PackageType = s.PackageType,
        WeightKg = s.WeightKg,
        PreferredWindow = s.PreferredWindow,
        SpecialNotes = s.SpecialNotes,
        CreatedAt = s.CreatedAt,
        Driver = driver,
        DriverLocation = driverLocation,
        Timeline = timeline,
        DeliveryCharge = s.Payment != null ? s.Payment.DeliveryCharge : 0m,
        PlatformFee = s.Payment != null ? s.Payment.PlatformFee : 0m,
        Cgst = s.Payment != null ? s.Payment.Cgst : 0m,
        Sgst = s.Payment != null ? s.Payment.Sgst : 0m,
        TotalAmount = s.Payment != null ? s.Payment.Amount : 0m,
        PaymentMethod = s.Payment != null ? s.Payment.Method.ToString() : string.Empty,
        PaymentStatus = s.Payment != null ? s.Payment.Status.ToString() : string.Empty
    };
}
