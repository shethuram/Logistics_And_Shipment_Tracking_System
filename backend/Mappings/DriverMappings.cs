using Logistics.Api.DTOs;
using Logistics.Api.Models;

namespace Logistics.Api.Mappings;

public static class DriverMappings
{
    public static PendingDriverDto ToPendingDriverDto(this Driver driver) => new()
    {
        Id = driver.Id,
        FullName = driver.User.FullName,
        Phone = driver.User.Phone,
        LicenseNumber = driver.LicenseNumber,
        Vehicles = driver.Vehicles.Select(v => v.ToPendingDriverVehicleDto()).ToList(),
        CreatedAt = driver.CreatedAt
    };

    public static ApproveDriverResponse ToApproveDriverResponse(this Driver driver) => new()
    {
        Id = driver.Id,
        ApprovalStatus = driver.ApprovalStatus,
        ApprovedAt = driver.ApprovedAt
    };

    public static DriverApprovalResponse ToDriverApprovalResponse(this Driver driver) => new()
    {
        Id = driver.Id,
        ApprovalStatus = driver.ApprovalStatus,
        ApprovalReason = driver.ApprovalReason
    };

    public static GoOnlineResponse ToGoOnlineResponse(this Driver driver, Vehicle activeVehicle) => new()
    {
        OperationalStatus = driver.OperationalStatus,
        ActiveVehicleType = activeVehicle.VehicleType
    };

    public static GoOfflineResponse ToGoOfflineResponse(this Driver driver) => new()
    {
        OperationalStatus = driver.OperationalStatus
    };

    public static AdminDriverDto ToAdminDriverDto(this Driver driver) => new()
    {
        Id = driver.Id,
        FullName = driver.User?.FullName ?? string.Empty,
        Phone = driver.User?.Phone ?? string.Empty,
        LicenseNumber = driver.LicenseNumber,
        ApprovalStatus = driver.ApprovalStatus,
        OperationalStatus = driver.OperationalStatus,
        Vehicles = driver.Vehicles?.Select(v => v.ToPendingDriverVehicleDto()).ToList() ?? new List<PendingDriverVehicleDto>(),
        CancelCount = driver.CancelCount,
        CreatedAt = driver.CreatedAt,
        LicenseFileUrl = driver.LicenseFileUrl,
        VerificationStatus = driver.VerificationStatus,
        VerificationReport = driver.VerificationReport,
        LicenseClasses = driver.LicenseClasses,
        AllowedVehicleTypes = driver.AllowedVehicleTypes
    };
}
