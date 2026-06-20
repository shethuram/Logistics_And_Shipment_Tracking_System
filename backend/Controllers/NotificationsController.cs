using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Logistics.Api.Exceptions;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUser _currentUser;
    private readonly IAuthorizationService _authorizationService;

    public NotificationsController(
        INotificationService notificationService,
        ICurrentUser currentUser,
        IAuthorizationService authorizationService)
    {
        _notificationService = notificationService;
        _currentUser = currentUser;
        _authorizationService = authorizationService;
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _notificationService.GetMyNotificationsAsync(_currentUser.Id, page, pageSize);
        return Ok(result);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var notification = await _notificationService.GetRawByIdAsync(id);
        var authResult = await _authorizationService.AuthorizeAsync(User, notification, "NotificationAccessPolicy");
        if (!authResult.Succeeded)
        {
            throw new ForbiddenException("You do not have access to this notification.");
        }

        var result = await _notificationService.MarkAsReadAsync(notification);
        return Ok(result);
    }
}
