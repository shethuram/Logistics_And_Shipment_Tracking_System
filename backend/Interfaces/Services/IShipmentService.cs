using Logistics.Api.DTOs;

namespace Logistics.Api.Interfaces.Services;

public interface IShipmentService
{
    // Batch 1 Methods
    Task<CreateShipmentResponse> CreateAsync(CreateShipmentRequest request, Guid customerId);
    Task<ShipmentResponse> GetByIdAsync(Guid id, Guid userId, string role);
    Task UpdateAsync(Guid id, UpdateShipmentRequest request, Guid customerId);
    Task<CancelShipmentResponse> CancelAsync(Guid id, Guid customerId);
    Task<PagedResult<ShipmentResponse>> GetShipmentsAsync(Guid userId, string role, string? search, string? status, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize);
    Task<IReadOnlyList<AvailableShipmentDto>> GetAvailableShipmentsAsync(Guid driverId);

    // Batch 2 Methods
    Task<ClaimShipmentResponse> ClaimAsync(Guid id, Guid driverId);
    Task<CancelClaimResponse> CancelClaimAsync(Guid id, Guid driverId, CancelClaimRequest request);
    Task<ConfirmPickupResponse> ConfirmPickupAsync(Guid id, Guid driverId, ConfirmPickupRequest request);
    Task<ConfirmDeliveryResponse> ConfirmDeliveryAsync(Guid id, Guid driverId, ConfirmDeliveryRequest request);
    Task<CashCollectedResponse> ConfirmCashCollectedAsync(Guid id, Guid driverId);
    Task<PickupFailedResponse> MarkPickupFailedAsync(Guid id, Guid driverId, PickupFailedRequest request);
    Task<PublicTrackingResponse> GetPublicTrackingAsync(string orderId, string phone, string date);
}
