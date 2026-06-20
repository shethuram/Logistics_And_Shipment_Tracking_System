using Logistics.Api.DTOs;
using Logistics.Api.Models;

namespace Logistics.Api.Interfaces.Services;

public interface IShipmentService
{
    Task<CreateShipmentResponse> CreateAsync(CreateShipmentRequest request, Guid customerId);
    Task<Shipment> GetRawByIdAsync(Guid id);
    Task<ShipmentResponse> GetByIdAsync(Shipment shipment);
    Task UpdateAsync(Shipment shipment, UpdateShipmentRequest request);
    Task<CancelShipmentResponse> CancelAsync(Shipment shipment, Guid userId);
    Task<PagedResult<ShipmentResponse>> GetShipmentsAsync(Guid userId, string role, string? search, ShipmentStatus? status, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize);
    Task<IReadOnlyList<AvailableShipmentDto>> GetAvailableShipmentsAsync(Guid driverId);

   
    Task<ClaimShipmentResponse> ClaimAsync(Guid id, Guid driverId);
    Task<CancelClaimResponse> CancelClaimAsync(Shipment shipment, Guid driverId, CancelClaimRequest request);
    Task<ConfirmPickupResponse> ConfirmPickupAsync(Shipment shipment, Guid driverId, ConfirmPickupRequest request);
    Task<ConfirmDeliveryResponse> ConfirmDeliveryAsync(Shipment shipment, Guid driverId, ConfirmDeliveryRequest request);
    Task<CashCollectedResponse> ConfirmCashCollectedAsync(Shipment shipment, Guid driverId);
    Task<PickupFailedResponse> MarkPickupFailedAsync(Shipment shipment, Guid driverId, PickupFailedRequest request);
    Task<PublicTrackingResponse> GetPublicTrackingAsync(string orderId, string phone, string date);
}
