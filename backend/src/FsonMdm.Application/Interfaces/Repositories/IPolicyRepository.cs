using FsonMdm.Domain.Entities;

namespace FsonMdm.Application.Interfaces.Repositories;

public interface IPolicyRepository
{
    /// <summary>Returns the single active (latest version) policy for a tenant, if any.</summary>
    Task<Policy?> GetLatestByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Policy policy, CancellationToken ct = default);
}
