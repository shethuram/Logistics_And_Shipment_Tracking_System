using Logistics.Api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUser _currentUser;

    public NotificationsController(INotificationService notificationService, ICurrentUser currentUser)
    {
        _notificationService = notificationService;
        _currentUser = currentUser;
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
        var result = await _notificationService.MarkAsReadAsync(id, _currentUser.Id);
        return Ok(result);
    }
}
