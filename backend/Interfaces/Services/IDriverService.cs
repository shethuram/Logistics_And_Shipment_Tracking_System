using Logistics.Api.DTOs;

namespace Logistics.Api.Interfaces.Services;

public interface IDriverService
{
    Task<GoOnlineResponse> GoOnlineAsync(Guid driverId, GoOnlineRequest request);
    Task<GoOfflineResponse> GoOfflineAsync(Guid driverId);
}
