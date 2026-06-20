using System;
using System.Collections.Generic;
using System.Security.Claims;
using Logistics.Api.Data;
using Logistics.Api.Models;
using Logistics.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Logistics.Api.Tests.Services;

public class CurrentUserTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly ClaimsPrincipal _userPrincipal;
    private readonly IDictionary<object, object?> _items;

    public CurrentUserTests()
    {

        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _httpContextMock = new Mock<HttpContext>();
        _items = new Dictionary<object, object?>();

        _httpContextMock.Setup(c => c.Items).Returns(_items);
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(_httpContextMock.Object);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public void Id_NotAuthenticated_ReturnsGuidEmpty()
    {

        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(c => c.User).Returns(principal);
        
        var currentUser = new CurrentUser(_httpContextAccessorMock.Object, _db);

        var result = currentUser.Id;

        Assert.Equal(Guid.Empty, result);
    }

    [Fact]
    public void Id_AuthenticatedButUserNotFound_ReturnsGuidEmpty()
    {

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "auth0|nonexistent")
        }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(c => c.User).Returns(principal);

        var currentUser = new CurrentUser(_httpContextAccessorMock.Object, _db);

        var result = currentUser.Id;

        Assert.Equal(Guid.Empty, result);
    }

    [Fact]
    public void Id_AuthenticatedAndUserExists_ReturnsUserIdAndCaches()
    {

        var auth0Id = "auth0|existing_user";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Auth0Id = auth0Id,
            FullName = "Test User",
            Email = "test@example.com",
            Phone = "1234567890",
            Role = UserRole.CUSTOMER
        };
        _db.Users.Add(user);
        _db.SaveChanges();

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, auth0Id)
        }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(c => c.User).Returns(principal);

        var currentUser = new CurrentUser(_httpContextAccessorMock.Object, _db);

        var result = currentUser.Id;

        Assert.Equal(user.Id, result);
        Assert.True(_items.ContainsKey("DbUserId"));
        Assert.Equal(user.Id, _items["DbUserId"]);
    }

    [Fact]
    public void Id_CachedInItems_ReturnsCachedIdWithoutQueryingDb()
    {

        var cachedId = Guid.NewGuid();
        _items["DbUserId"] = cachedId;

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "auth0|some_id")
        }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(c => c.User).Returns(principal);

        var currentUser = new CurrentUser(_httpContextAccessorMock.Object, _db);

        var result = currentUser.Id;

        Assert.Equal(cachedId, result);
    }

    [Fact]
    public void Role_NotAuthenticated_ReturnsEmptyString()
    {

        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(c => c.User).Returns(principal);

        var currentUser = new CurrentUser(_httpContextAccessorMock.Object, _db);

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

        var currentUser = new CurrentUser(_httpContextAccessorMock.Object, _db);

        var result = currentUser.Role;

        Assert.Equal("DRIVER", result);
    }


}
