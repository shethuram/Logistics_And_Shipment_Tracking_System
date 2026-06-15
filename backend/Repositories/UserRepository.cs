using Logistics.Api.Data;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExistsByAuth0IdAsync(string auth0Id) =>
        _db.Users.AnyAsync(u => u.Auth0Id == auth0Id);

    public async Task<User> AddAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }
}
