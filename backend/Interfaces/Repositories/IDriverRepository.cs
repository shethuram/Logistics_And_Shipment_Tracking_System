using Logistics.Api.Models;

namespace Logistics.Api.Interfaces.Repositories;

public interface IDriverRepository
{
    Task<Driver> AddAsync(Driver driver);
    Task<(IReadOnlyList<Driver> Items, int Total)> GetByApprovalStatusAsync(ApprovalStatus status, int page, int pageSize);
    Task<(IReadOnlyList<Driver> Items, int Total)> GetDriversAsync(ApprovalStatus? status, int page, int pageSize);
    Task<Driver?> GetByIdAsync(Guid id);
    Task<Driver?> GetByIdWithUserAndVehiclesAsync(Guid id);
    Task<Driver?> GetByUserIdAsync(Guid userId);
    Task UpdateAsync(Driver driver);
}
