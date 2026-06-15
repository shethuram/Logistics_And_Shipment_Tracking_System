using Logistics.Api.DTOs;

namespace Logistics.Api.Interfaces.Services;

public interface IDisputeService
{
    Task<RaiseDisputeResponse> RaiseDisputeAsync(RaiseDisputeRequest request, Guid customerId);
    Task<PagedResult<DisputeAdminDto>> GetDisputesAsync(string? status, int page, int pageSize, string role);
    Task<ResolveDisputeResponse> ResolveDisputeAsync(Guid id, ResolveDisputeRequest request, Guid adminUserId, string role);
}
