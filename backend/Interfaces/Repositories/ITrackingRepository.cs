using Logistics.Api.Models;

namespace Logistics.Api.Interfaces.Repositories;

public interface ITrackingRepository
{
    Task AddAsync(Tracking tracking);
    Task<IReadOnlyList<Tracking>> GetHistoryAsync(Guid shipmentId);
    Task<Tracking?> GetLatestPingAsync(Guid shipmentId);
}
