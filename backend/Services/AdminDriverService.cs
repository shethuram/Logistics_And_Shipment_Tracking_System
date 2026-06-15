using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Mappings;
using Logistics.Api.Models;

namespace Logistics.Api.Services;

public class AdminDriverService : IAdminDriverService
{
    private readonly IDriverRepository _driverRepo;

    public AdminDriverService(IDriverRepository driverRepo)
    {
        _driverRepo = driverRepo;
    }

    public async Task<PagedResult<PendingDriverDto>> GetPendingDriversAsync(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var (items, total) = await _driverRepo.GetByApprovalStatusAsync(ApprovalStatus.PENDING, page, pageSize);

        var data = items.Select(d => d.ToPendingDriverDto()).ToList();

        return new PagedResult<PendingDriverDto>
        {
            Data = data,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ApproveDriverResponse> ApproveDriverAsync(Guid id)
    {
        var driver = await GetOrThrowAsync(id);

        driver.ApprovalStatus = ApprovalStatus.APPROVED;
        driver.ApprovalReason = null;
        driver.ApprovedAt = DateTime.UtcNow;

        await _driverRepo.UpdateAsync(driver);

        return driver.ToApproveDriverResponse();
    }

    public async Task<DriverApprovalResponse> RejectDriverAsync(Guid id, RejectDriverRequest request)
    {
        var driver = await GetOrThrowAsync(id);

        if (driver.ApprovalStatus != ApprovalStatus.PENDING)
            throw new BusinessRuleException("Only pending drivers can be rejected.");

        driver.ApprovalStatus = ApprovalStatus.REJECTED;
        driver.ApprovalReason = request.Reason;

        await _driverRepo.UpdateAsync(driver);

        return driver.ToDriverApprovalResponse();
    }

    public async Task<DriverApprovalResponse> SuspendDriverAsync(Guid id, SuspendDriverRequest request)
    {
        var driver = await GetOrThrowAsync(id);

        if (driver.ApprovalStatus != ApprovalStatus.APPROVED)
            throw new BusinessRuleException("Only approved drivers can be suspended.");

        driver.ApprovalStatus = ApprovalStatus.SUSPENDED;
        driver.ApprovalReason = request.Reason;

        await _driverRepo.UpdateAsync(driver);

        return driver.ToDriverApprovalResponse();
    }

    private async Task<Driver> GetOrThrowAsync(Guid id)
    {
        var driver = await _driverRepo.GetByIdAsync(id);
        if (driver is null)
            throw new NotFoundException("Driver not found.");
        return driver;
    }
}
