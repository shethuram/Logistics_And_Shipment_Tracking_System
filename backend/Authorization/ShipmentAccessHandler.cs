using System.Threading.Tasks;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Models;
using Microsoft.AspNetCore.Authorization;

namespace Logistics.Api.Authorization;

public class ShipmentAccessHandler : AuthorizationHandler<ShipmentAccessRequirement, Shipment>
{
    private readonly ICurrentUser _currentUser;

    public ShipmentAccessHandler(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ShipmentAccessRequirement requirement,
        Shipment resource)
    {
        if (_currentUser.Role == "ADMIN")
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (_currentUser.Role == "CUSTOMER" && resource.CustomerId == _currentUser.Id)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (_currentUser.Role == "DRIVER" && resource.DriverId == _currentUser.DriverId)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}
