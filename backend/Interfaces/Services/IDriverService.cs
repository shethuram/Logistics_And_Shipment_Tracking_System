using Logistics.Api.DTOs;
using Microsoft.AspNetCore.Http;

namespace Logistics.Api.Interfaces.Services;

public interface IDriverService
{
    Task<GoOnlineResponse> GoOnlineAsync(Guid driverId, GoOnlineRequest request);
    Task<GoOfflineResponse> GoOfflineAsync(Guid driverId);
    Task UpdateDriverProfileAsync(Guid driverId, UpdateDriverProfileRequest request, IFormFile? licenseFile);
}
