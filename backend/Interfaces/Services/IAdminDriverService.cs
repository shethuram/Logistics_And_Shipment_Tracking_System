using Logistics.Api.DTOs;
using Logistics.Api.Models;

namespace Logistics.Api.Interfaces.Services;

public interface IAdminDriverService
{
    Task<PagedResult<PendingDriverDto>> GetPendingDriversAsync(int page, int pageSize);
    Task<PagedResult<AdminDriverDto>> GetDriversAsync(ApprovalStatus? status, int page, int pageSize);
    Task<AdminDriverDto?> GetDriverByIdAsync(Guid id);
    Task<ApproveDriverResponse> ApproveDriverAsync(Guid id);
    Task<DriverApprovalResponse> RejectDriverAsync(Guid id, RejectDriverRequest request);
    Task<DriverApprovalResponse> SuspendDriverAsync(Guid id, SuspendDriverRequest request);
}
