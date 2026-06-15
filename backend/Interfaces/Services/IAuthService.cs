using Logistics.Api.DTOs;

namespace Logistics.Api.Interfaces.Services;

public interface IAuthService
{
    Task<RegisterCustomerResponse> RegisterCustomerAsync(RegisterCustomerRequest request);
    Task<RegisterDriverResponse> RegisterDriverAsync(RegisterDriverRequest request);
}
