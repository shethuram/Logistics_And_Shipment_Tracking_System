using System;
using System.Security.Claims;
using Logistics.Api.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Logistics.Api.Tests.Services;

public class CurrentUserTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<HttpContext> _httpContextMock;

    public CurrentUserTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _httpContextMock = new Mock<HttpContext>();
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(_httpContextMock.Object);
    }

    [Fact]
    public void Id_NotAuthenticated_ReturnsGuidEmpty()
    {
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(c => c.User).Returns(principal);
        
        var currentUser = new CurrentUser(_httpContextAccessorMock.Object);

        var result = currentUser.Id;

        Assert.Equal(Guid.Empty, result);
    }

    [Fact]
    public void Id_AuthenticatedWithUserIdClaim_ReturnsUserId()
    {
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("https://logistics.api/claims/user_id", userId.ToString())
        }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(c => c.User).Returns(principal);

        var currentUser = new CurrentUser(_httpContextAccessorMock.Object);

        var result = currentUser.Id;

        Assert.Equal(userId, result);
    }

    [Fact]
    public void Role_NotAuthenticated_ReturnsEmptyString()
    {
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(c => c.User).Returns(principal);

        var currentUser = new CurrentUser(_httpContextAccessorMock.Object);

        var result = currentUser.Role;

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Role_AuthenticatedWithClaim_ReturnsRoleFromClaim()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("https://logistics.api/claims/roles", "DRIVER")
        }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(c => c.User).Returns(principal);

        var currentUser = new CurrentUser(_httpContextAccessorMock.Object);

        var result = currentUser.Role;

        Assert.Equal("DRIVER", result);
    }

    [Fact]
    public void DriverId_AuthenticatedWithDriverIdClaim_ReturnsDriverId()
    {
        var driverId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("https://logistics.api/claims/driver_id", driverId.ToString())
        }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(c => c.User).Returns(principal);

        var currentUser = new CurrentUser(_httpContextAccessorMock.Object);

        var result = currentUser.DriverId;

        Assert.Equal(driverId, result);
    }
}
