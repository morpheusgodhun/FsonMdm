using FsonMdm.Domain.Entities;

namespace FsonMdm.Application.Interfaces.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Tenant?> GetByEnrollmentKeyAsync(string enrollmentKey, CancellationToken ct = default);
}
