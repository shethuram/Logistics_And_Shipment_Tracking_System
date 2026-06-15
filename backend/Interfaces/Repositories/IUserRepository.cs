using Logistics.Api.Models;

namespace Logistics.Api.Interfaces.Repositories;

public interface IUserRepository
{
    Task<bool> ExistsByAuth0IdAsync(string auth0Id);
    Task<User> AddAsync(User user);
}
