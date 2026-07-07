using Logistics.Api.DTOs;
using Logistics.Api.Models;

namespace Logistics.Api.Interfaces.Services;

public interface IDisputeService
{
    Task<RaiseDisputeResponse> RaiseDisputeAsync(Shipment shipment, string complaintText, Guid customerId);
    Task<PagedResult<DisputeAdminDto>> GetDisputesAsync(DisputeStatus? status, int page, int pageSize);
    Task<ResolveDisputeResponse> ResolveDisputeAsync(Guid id, ResolveDisputeRequest request, Guid adminUserId);
    Task<IEnumerable<DisputeResponse>> GetMyDisputesAsync(Guid customerId);
}
