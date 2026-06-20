using System.Threading.Tasks;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Models;
using Microsoft.AspNetCore.Authorization;

namespace Logistics.Api.Authorization;

public class NotificationAccessHandler : AuthorizationHandler<NotificationAccessRequirement, Notification>
{
    private readonly ICurrentUser _currentUser;

    public NotificationAccessHandler(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        NotificationAccessRequirement requirement,
        Notification resource)
    {
        if (resource.UserId == _currentUser.Id)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
