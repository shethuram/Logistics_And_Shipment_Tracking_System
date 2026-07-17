using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Models;
using Logistics.Api.Services;
using Moq;
using Xunit;

using Microsoft.Extensions.Logging;

namespace Logistics.Api.Tests.Services;

public class AdminDriverServiceTests
{
    private readonly Mock<IDriverRepository> _driverRepoMock;
    private readonly AdminDriverService _service;

    public AdminDriverServiceTests()
    {
        _driverRepoMock = new Mock<IDriverRepository>();
        _service = new AdminDriverService(_driverRepoMock.Object, new Mock<ILogger<AdminDriverService>>().Object);
    }

    [Fact]
    public async Task GetPendingDriversAsync_AdjustsPagesAndReturnsData()
    {
        var drivers = new List<Driver>
        {
            new Driver { Id = Guid.NewGuid(), ApprovalStatus = ApprovalStatus.PENDING, LicenseNumber = "L1", User = new User { FullName = "Name" } }
        };
        _driverRepoMock.Setup(r => r.GetByApprovalStatusAsync(ApprovalStatus.PENDING, 1, 20)).ReturnsAsync((drivers, 1));

        var result = await _service.GetPendingDriversAsync(0, 0);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(1, result.Total);
        Assert.Single(result.Data);
    }

    [Fact]
    public async Task ApproveDriverAsync_DriverNotFound_ThrowsNotFoundException()
    {
        var driverId = Guid.NewGuid();
        _driverRepoMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync((Driver)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.ApproveDriverAsync(driverId));
    }

    [Fact]
    public async Task ApproveDriverAsync_DriverExists_ApprovesDriver()
    {
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, ApprovalStatus = ApprovalStatus.PENDING };
        _driverRepoMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);

        var result = await _service.ApproveDriverAsync(driverId);

        Assert.Equal(driverId, result.Id);
        Assert.Equal(ApprovalStatus.APPROVED, result.ApprovalStatus);
        Assert.Equal(ApprovalStatus.APPROVED, driver.ApprovalStatus);
        Assert.True(driver.ApprovedAt > DateTime.UtcNow.AddSeconds(-5));
        _driverRepoMock.Verify(r => r.UpdateAsync(driver), Times.Once);
    }

    [Fact]
    public async Task RejectDriverAsync_DriverExists_RejectsDriver()
    {
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, ApprovalStatus = ApprovalStatus.PENDING };
        _driverRepoMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);
        var request = new RejectDriverRequest { Reason = "Invalid ID" };

        var result = await _service.RejectDriverAsync(driverId, request);

        Assert.Equal(driverId, result.Id);
        Assert.Equal(ApprovalStatus.REJECTED, result.ApprovalStatus);
        Assert.Equal("Invalid ID", result.ApprovalReason);
        Assert.Equal(ApprovalStatus.REJECTED, driver.ApprovalStatus);
        _driverRepoMock.Verify(r => r.UpdateAsync(driver), Times.Once);
    }

    [Fact]
    public async Task SuspendDriverAsync_DriverExists_SuspendsDriver()
    {
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, ApprovalStatus = ApprovalStatus.APPROVED };
        _driverRepoMock.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);
        var request = new SuspendDriverRequest { Reason = "Abuse" };

        var result = await _service.SuspendDriverAsync(driverId, request);

        Assert.Equal(driverId, result.Id);
        Assert.Equal(ApprovalStatus.SUSPENDED, result.ApprovalStatus);
        Assert.Equal("Abuse", result.ApprovalReason);
        Assert.Equal(ApprovalStatus.SUSPENDED, driver.ApprovalStatus);
        _driverRepoMock.Verify(r => r.UpdateAsync(driver), Times.Once);
    }
}
