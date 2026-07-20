using Logistics.Api.DTOs;
using Microsoft.AspNetCore.Http;

namespace Logistics.Api.Interfaces.Services;

public interface IAuthService
{
    Task<RegisterCustomerResponse> RegisterCustomerAsync(RegisterCustomerRequest request);
    Task<RegisterDriverResponse> RegisterDriverAsync(RegisterDriverRequest request, IFormFile licenseFile);
}
