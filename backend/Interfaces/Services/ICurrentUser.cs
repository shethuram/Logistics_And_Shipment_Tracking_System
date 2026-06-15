namespace Logistics.Api.Interfaces.Services;

public interface ICurrentUser
{
    Guid Id { get; }
    string Role { get; }
}
