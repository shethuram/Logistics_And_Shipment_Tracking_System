using Logistics.Api.DTOs;

namespace Logistics.Api.Interfaces.Services;

public interface IAdminDriverService
{
    Task<PagedResult<PendingDriverDto>> GetPendingDriversAsync(int page, int pageSize);
    Task<ApproveDriverResponse> ApproveDriverAsync(Guid id);
    Task<DriverApprovalResponse> RejectDriverAsync(Guid id, RejectDriverRequest request);
    Task<DriverApprovalResponse> SuspendDriverAsync(Guid id, SuspendDriverRequest request);
}
