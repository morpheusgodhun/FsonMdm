using FsonMdm.Domain.Entities;

namespace FsonMdm.Application.Interfaces.Repositories;

public interface IManagedAppRepository
{
    Task AddAsync(ManagedApp app, CancellationToken ct = default);
    Task<ManagedApp?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ManagedApp>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default);
}
