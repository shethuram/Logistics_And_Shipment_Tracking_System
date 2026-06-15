using Logistics.Api.DTOs;
using Logistics.Api.Models;

namespace Logistics.Api.Mappings;

public static class ShipmentMappings
{
    public static CreateShipmentResponse ToCreateShipmentResponse(this Shipment s, string? senderOtp = null, string? paymentUrl = null) => new()
    {
        Id = s.Id,
        OrderId = s.OrderId,
        Status = s.Status.ToString(),
        PaymentUrl = paymentUrl,
        SenderOtp = senderOtp
    };

    public static ShipmentResponse ToShipmentResponse(this Shipment s) => new()
    {
        Id = s.Id,
        OrderId = s.OrderId,
        Status = s.Status.ToString(),
        PickupAddress = s.PickupAddress,
        PickupLat = s.PickupLat,
        PickupLng = s.PickupLng,
        DropAddress = s.DropAddress,
        DropLat = s.DropLat,
        DropLng = s.DropLng,
        ReceiverName = s.ReceiverName,
        ReceiverPhone = s.ReceiverPhone,
        PackageType = s.PackageType.ToString(),
        WeightKg = s.WeightKg,
        PreferredWindow = s.PreferredWindow?.ToString(),
        SpecialNotes = s.SpecialNotes,
        DriverInstruction = s.DriverInstruction,
        RiskFlag = s.RiskFlag,
        RiskSeverity = s.RiskSeverity.ToString(),
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
            VehicleType = s.Driver.ActiveVehicle?.VehicleType.ToString() ?? string.Empty,
            VehicleNumber = s.Driver.ActiveVehicle?.VehicleNumber ?? string.Empty
        } : null
    };

    public static AvailableShipmentDto ToAvailableShipmentDto(this Shipment s, double distanceToPickupKm) => new()
    {
        Id = s.Id,
        OrderId = s.OrderId,
        PickupAddress = s.PickupAddress,
        DropAddress = s.DropAddress,
        PackageType = s.PackageType.ToString(),
        WeightKg = s.WeightKg,
        PreferredWindow = s.PreferredWindow?.ToString(),
        SenderPhone = s.Customer?.Phone ?? string.Empty,
        ReceiverPhone = s.ReceiverPhone,
        DistanceToPickupKm = distanceToPickupKm,
        DriverInstruction = s.DriverInstruction
    };

    public static ClaimShipmentResponse ToClaimShipmentResponse(this Shipment s) => new()
    {
        Id = s.Id,
        Status = s.Status.ToString(),
        OrderId = s.OrderId
    };

    public static CancelClaimResponse ToCancelClaimResponse(this Shipment s, int driverCancelCount) => new()
    {
        Id = s.Id,
        Status = s.Status.ToString(),
        DriverCancelCount = driverCancelCount
    };

    public static ConfirmPickupResponse ToConfirmPickupResponse(this Shipment s) => new()
    {
        Id = s.Id,
        Status = s.Status.ToString()
    };

    public static ConfirmDeliveryResponse ToConfirmDeliveryResponse(this Shipment s) => new()
    {
        Id = s.Id,
        Status = s.Status.ToString(),
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
        Status = s.Status.ToString()
    };

    public static PublicTrackingResponse ToPublicTrackingResponse(
        this Shipment s, 
        string receiverOtp, 
        PublicTrackingDriverDto? driver, 
        DriverLocationDto? driverLocation, 
        List<TimelineEntryDto> timeline) => new()
    {
        Id = s.Id,
        OrderId = s.OrderId,
        Status = s.Status.ToString(),
        PickupAddress = s.PickupAddress,
        DropAddress = s.DropAddress,
        PackageType = s.PackageType.ToString(),
        WeightKg = s.WeightKg,
        PreferredWindow = s.PreferredWindow?.ToString(),
        SpecialNotes = s.SpecialNotes,
        CreatedAt = s.CreatedAt,
        ReceiverOtp = receiverOtp,
        Driver = driver,
        DriverLocation = driverLocation,
        Timeline = timeline
    };
}
