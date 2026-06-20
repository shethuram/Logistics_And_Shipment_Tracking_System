using System;
using System.Threading.Tasks;
using Logistics.Api.Data;
using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Models;
using Logistics.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Logistics.Api.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IDriverRepository> _driverRepoMock;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _userRepoMock = new Mock<IUserRepository>();
        _driverRepoMock = new Mock<IDriverRepository>();

        _service = new AuthService(
            _userRepoMock.Object,
            _driverRepoMock.Object,
            _db
        );
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task RegisterCustomerAsync_Auth0IdExists_ThrowsConflictException()
    {
        var auth0Id = "auth0|customer1";
        _userRepoMock.Setup(r => r.ExistsByAuth0IdAsync(auth0Id)).ReturnsAsync(true);
        var request = new RegisterCustomerRequest { Auth0Id = auth0Id, FullName = "Name", Email = "e", Phone = "p" };

        await Assert.ThrowsAsync<ConflictException>(() => _service.RegisterCustomerAsync(request));
    }

    [Fact]
    public async Task RegisterCustomerAsync_NewUser_CreatesCustomer()
    {
        var auth0Id = "auth0|customer1";
        _userRepoMock.Setup(r => r.ExistsByAuth0IdAsync(auth0Id)).ReturnsAsync(false);
        var request = new RegisterCustomerRequest { Auth0Id = auth0Id, FullName = "Name", Email = "email@example.com", Phone = "123" };

        var result = await _service.RegisterCustomerAsync(request);

        Assert.NotNull(result);
        Assert.Equal("Name", result.FullName);
        Assert.Equal(UserRole.CUSTOMER, result.Role);

        _userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u => 
            u.Auth0Id == auth0Id && 
            u.FullName == "Name" && 
            u.Role == UserRole.CUSTOMER
        )), Times.Once);
    }

    [Fact]
    public async Task RegisterDriverAsync_Auth0IdExists_ThrowsConflictException()
    {
        var auth0Id = "auth0|driver1";
        _userRepoMock.Setup(r => r.ExistsByAuth0IdAsync(auth0Id)).ReturnsAsync(true);
        var request = new RegisterDriverRequest { Auth0Id = auth0Id, FullName = "Name", Email = "e", Phone = "p", LicenseNumber = "L1" };

        await Assert.ThrowsAsync<ConflictException>(() => _service.RegisterDriverAsync(request));
    }

    [Fact]
    public async Task RegisterDriverAsync_NewUser_CreatesUserAndDriverWithTransaction()
    {
        var auth0Id = "auth0|driver1";
        _userRepoMock.Setup(r => r.ExistsByAuth0IdAsync(auth0Id)).ReturnsAsync(false);
        var request = new RegisterDriverRequest 
        { 
            Auth0Id = auth0Id, 
            FullName = "Driver Name", 
            Email = "email@example.com", 
            Phone = "123", 
            LicenseNumber = "TN33AB9999" 
        };

        var result = await _service.RegisterDriverAsync(request);

        Assert.NotNull(result);
        Assert.Equal("Driver Name", result.FullName);
        Assert.Equal(ApprovalStatus.PENDING, result.ApprovalStatus);

        _userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u => 
            u.Auth0Id == auth0Id && 
            u.FullName == "Driver Name" && 
            u.Role == UserRole.DRIVER
        )), Times.Once);

        _driverRepoMock.Verify(r => r.AddAsync(It.Is<Driver>(d => 
            d.LicenseNumber == "TN33AB9999" && 
            d.ApprovalStatus == ApprovalStatus.PENDING
        )), Times.Once);
    }

    [Fact]
    public async Task RegisterDriverAsync_ErrorOccurs_RollsBackTransaction()
    {
        var auth0Id = "auth0|driver_err";
        _userRepoMock.Setup(r => r.ExistsByAuth0IdAsync(auth0Id)).ReturnsAsync(false);
        var request = new RegisterDriverRequest 
        { 
            Auth0Id = auth0Id, 
            FullName = "Driver Name", 
            Email = "email@example.com", 
            Phone = "123", 
            LicenseNumber = "TN33AB9999" 
        };

        _driverRepoMock.Setup(r => r.AddAsync(It.IsAny<Driver>())).ThrowsAsync(new Exception("Database crash"));

        await Assert.ThrowsAsync<Exception>(() => _service.RegisterDriverAsync(request));

        var usersCount = await _db.Users.CountAsync();
        Assert.Equal(0, usersCount);
    }
}
