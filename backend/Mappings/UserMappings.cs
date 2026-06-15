using Logistics.Api.DTOs;
using Logistics.Api.Models;

namespace Logistics.Api.Mappings;

public static class UserMappings
{
    public static RegisterCustomerResponse ToRegisterCustomerResponse(this User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Role = user.Role.ToString()
    };

    public static RegisterDriverResponse ToRegisterDriverResponse(this User user, Driver driver) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        ApprovalStatus = driver.ApprovalStatus.ToString()
    };
}
