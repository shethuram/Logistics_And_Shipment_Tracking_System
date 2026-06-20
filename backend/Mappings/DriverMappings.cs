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
}
