using Microsoft.Extensions.Logging;
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
    private readonly ILogger<AdminDriverService> _logger;
    private readonly INotificationService _notificationService;

    public AdminDriverService(
        IDriverRepository driverRepo,
        ILogger<AdminDriverService> logger,
        INotificationService notificationService)
    {
        _driverRepo = driverRepo;
        _logger = logger;
        _notificationService = notificationService;
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

    public async Task<PagedResult<AdminDriverDto>> GetDriversAsync(ApprovalStatus? status, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var (items, total) = await _driverRepo.GetDriversAsync(status, page, pageSize);

        var data = items.Select(d => d.ToAdminDriverDto()).ToList();

        return new PagedResult<AdminDriverDto>
        {
            Data = data,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminDriverDto?> GetDriverByIdAsync(Guid id)
    {
        var driver = await _driverRepo.GetByIdWithUserAndVehiclesAsync(id);
        if (driver == null) return null;
        return driver.ToAdminDriverDto();
    }

    public async Task<ApproveDriverResponse> ApproveDriverAsync(Guid id)
    {
        var driver = await GetOrThrowAsync(id);

        driver.ApprovalStatus = ApprovalStatus.APPROVED;
        driver.ApprovalReason = null;
        driver.ApprovedAt = DateTime.UtcNow;

        await _driverRepo.UpdateAsync(driver);
        _logger.LogInformation("Driver registration {DriverId} approved by Admin.", driver.Id);

        await _notificationService.CreateNotificationAsync(
            driver.UserId,
            null,
            "Profile Approved",
            "Your driver registration has been approved by the Admin. You can now configure your vehicle and go online!"
        );

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
        _logger.LogWarning("Driver registration {DriverId} rejected by Admin. Reason: {Reason}", driver.Id, request.Reason);

        await _notificationService.CreateNotificationAsync(
            driver.UserId,
            null,
            "Profile Rejected",
            $"Your driver registration has been rejected by the Admin. Reason: {request.Reason}. You can resubmit your details."
        );

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
        _logger.LogWarning("Active Driver {DriverId} suspended by Admin. Reason: {Reason}", driver.Id, request.Reason);

        await _notificationService.CreateNotificationAsync(
            driver.UserId,
            null,
            "Profile Suspended",
            $"Your driver profile has been suspended by the Admin. Reason: {request.Reason}. Please contact support."
        );

        return driver.ToDriverApprovalResponse();
    }

    public async Task UpdateDriverVerificationAsync(Guid id, UpdateDriverVerificationRequest request)
    {
        var driver = await GetOrThrowAsync(id);

        driver.VerificationStatus = request.VerificationStatus;
        driver.VerificationReport = request.VerificationReport;
        driver.LicenseClasses = request.LicenseClasses;
        driver.AllowedVehicleTypes = request.AllowedVehicleTypes;

        await _driverRepo.UpdateAsync(driver);
        _logger.LogInformation("Driver {DriverId} verification updated by AI Agent to: {Status}", driver.Id, request.VerificationStatus);

        var statusFriendly = request.VerificationStatus.ToString().Replace("_", " ");
        
        await _notificationService.CreateNotificationAsync(
            driver.UserId,
            null,
            "AI License Verification Update",
            $"Your license verification status is now: {statusFriendly}."
        );

        await _notificationService.CreateAdminNotificationAsync(
            null,
            "AI Verification Alert",
            $"AI verification status for driver {driver.LicenseNumber} is {statusFriendly} - please approve or reject."
        );

        await _notificationService.BroadcastAdminAlertAsync("DRIVER_VERIFICATION_UPDATE", new { DriverId = driver.Id, Status = driver.VerificationStatus.ToString() });
    }

    private async Task<Driver> GetOrThrowAsync(Guid id)
    {
        var driver = await _driverRepo.GetByIdAsync(id);
        if (driver is null)
            throw new NotFoundException("Driver not found.");
        return driver;
    }
}
