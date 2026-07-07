namespace Logistics.Api.DTOs;

public record NotificationDto
{
    public Guid Id { get; init; }
    public Guid? ShipmentId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record MyNotificationsResponse
{
    public IReadOnlyList<NotificationDto> Data { get; init; } = Array.Empty<NotificationDto>();
    public int UnreadCount { get; init; }
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public record MarkReadResponse
{
    public Guid Id { get; init; }
    public bool IsRead { get; init; }
}
