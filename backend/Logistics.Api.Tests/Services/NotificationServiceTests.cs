using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Hubs;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Models;
using Logistics.Api.Services;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace Logistics.Api.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _notificationRepoMock;
    private readonly Mock<IHubContext<TrackingHub>> _hubContextMock;
    private readonly Mock<IHubClients> _clientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _notificationRepoMock = new Mock<INotificationRepository>();
        _hubContextMock = new Mock<IHubContext<TrackingHub>>();
        _clientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();

        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);

        _service = new NotificationService(
            _notificationRepoMock.Object,
            null!,
            _hubContextMock.Object
        );
    }

    [Fact]
    public async Task CreateNotificationAsync_SavesToRepoAndSendsSignalR()
    {
        var userId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var title = "Test Notification";
        var message = "Hello World";

        await _service.CreateNotificationAsync(userId, shipmentId, title, message);

        _notificationRepoMock.Verify(r => r.AddAsync(It.Is<Notification>(n => 
            n.UserId == userId && 
            n.ShipmentId == shipmentId && 
            n.Title == title && 
            n.Message == message && 
            !n.IsRead
        )), Times.Once);

        _clientsMock.Verify(c => c.Group($"user-{userId}"), Times.Once);
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "notificationReceived",
            It.Is<object[]>(args => args.Length == 1),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task BroadcastShipmentUpdateAsync_SendsSignalR()
    {
        var shipmentId = Guid.NewGuid();
        var status = "ASSIGNED";
        var data = new { DriverId = Guid.NewGuid() };

        await _service.BroadcastShipmentUpdateAsync(shipmentId, status, data);

        _clientsMock.Verify(c => c.Group($"shipment-{shipmentId}"), Times.Once);
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "shipmentUpdated",
            It.Is<object[]>(args => args.Length == 1),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task BroadcastNewJobAlertAsync_SendsSignalR()
    {
        var vehicleType = "TWO_WHEELER";
        var data = new { ShipmentId = Guid.NewGuid() };

        await _service.BroadcastNewJobAlertAsync(vehicleType, data);

        _clientsMock.Verify(c => c.Group($"vehicle-{vehicleType}"), Times.Once);
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "newJobAlert",
            It.Is<object[]>(args => args[0] == data),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task BroadcastAdminAlertAsync_SendsSignalR()
    {
        var alertType = "HIGH_CANCEL_COUNT";
        var data = new { DriverId = Guid.NewGuid() };

        await _service.BroadcastAdminAlertAsync(alertType, data);

        _clientsMock.Verify(c => c.Group("admin"), Times.Once);
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "adminAlert",
            It.Is<object[]>(args => args.Length == 1),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task BroadcastDriverLocationAsync_SendsSignalR()
    {
        var shipmentId = Guid.NewGuid();
        decimal lat = 12.34m;
        decimal lng = 56.78m;

        await _service.BroadcastDriverLocationAsync(shipmentId, lat, lng);

        _clientsMock.Verify(c => c.Group($"shipment-{shipmentId}"), Times.Once);
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "locationUpdated",
            It.Is<object[]>(args => args.Length == 1),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task GetMyNotificationsAsync_ReturnsPagedList()
    {
        var userId = Guid.NewGuid();
        var notifications = new List<Notification>
        {
            new Notification { Id = Guid.NewGuid(), UserId = userId, Title = "Title", Message = "Msg", IsRead = false, CreatedAt = DateTime.UtcNow }
        };
        _notificationRepoMock.Setup(r => r.GetByUserIdAsync(userId, 1, 10))
            .ReturnsAsync((notifications, 1, 1));

        var result = await _service.GetMyNotificationsAsync(userId, 1, 10);

        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.Equal(1, result.UnreadCount);
        Assert.Single(result.Data);
        Assert.Equal("Title", result.Data[0].Title);
    }

    [Fact]
    public async Task MarkAsReadAsync_NotificationNotFound_ThrowsNotFoundException()
    {
        var notifId = Guid.NewGuid();
        _notificationRepoMock.Setup(r => r.GetByIdAsync(notifId)).ReturnsAsync((Notification)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.MarkAsReadAsync(notifId, Guid.NewGuid()));
    }

    [Fact]
    public async Task MarkAsReadAsync_NotAuthorizedUser_ThrowsForbiddenException()
    {
        var notifId = Guid.NewGuid();
        var notification = new Notification { Id = notifId, UserId = Guid.NewGuid() };
        _notificationRepoMock.Setup(r => r.GetByIdAsync(notifId)).ReturnsAsync(notification);

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.MarkAsReadAsync(notifId, Guid.NewGuid()));
    }

    [Fact]
    public async Task MarkAsReadAsync_AuthorizedUser_MarksReadAndUpdates()
    {
        var notifId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var notification = new Notification { Id = notifId, UserId = userId, IsRead = false };
        _notificationRepoMock.Setup(r => r.GetByIdAsync(notifId)).ReturnsAsync(notification);

        var result = await _service.MarkAsReadAsync(notifId, userId);

        Assert.NotNull(result);
        Assert.True(result.IsRead);
        Assert.True(notification.IsRead);
        _notificationRepoMock.Verify(r => r.UpdateAsync(notification), Times.Once);
    }
}
