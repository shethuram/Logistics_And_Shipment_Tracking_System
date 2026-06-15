using Logistics.Api.Models;

namespace Logistics.Api.Interfaces.Repositories;

public interface IShipmentRepository
{
    Task<Shipment> AddAsync(Shipment shipment);
    Task<Shipment?> GetByIdAsync(Guid id);
    Task<Shipment?> GetByIdForUpdateAsync(Guid id);
    Task UpdateAsync(Shipment shipment);
    Task<(IReadOnlyList<Shipment> Items, int Total)> GetShipmentsAsync(
        Guid? customerId,
        string? search,
        ShipmentStatus? status,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize);
    Task<IReadOnlyList<Shipment>> GetOpenShipmentsAsync();
    Task<int> GetTodayCountAsync(string datePrefix);
    Task<Shipment?> GetActiveShipmentForDriverAsync(Guid driverId);
    Task<Shipment?> GetByPublicTrackParamsAsync(string orderId, string phone);
}
