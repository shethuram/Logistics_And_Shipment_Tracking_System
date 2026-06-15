using Logistics.Api.Models;

namespace Logistics.Api.Interfaces.Repositories;

public interface IDisputeRepository
{
    Task AddAsync(Dispute dispute);
    Task<Dispute?> GetByIdAsync(Guid id);
    Task<(IReadOnlyList<Dispute> Items, int Total)> GetDisputesAsync(DisputeStatus? status, int page, int pageSize);
    Task UpdateAsync(Dispute dispute);
}
