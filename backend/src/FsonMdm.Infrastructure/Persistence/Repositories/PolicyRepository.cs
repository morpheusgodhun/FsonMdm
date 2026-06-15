using FsonMdm.Application.Interfaces.Repositories;
using FsonMdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FsonMdm.Infrastructure.Persistence.Repositories;

public class PolicyRepository : IPolicyRepository
{
    private readonly AppDbContext _db;
    public PolicyRepository(AppDbContext db) => _db = db;

    public Task<Policy?> GetLatestByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        _db.Policies.Where(p => p.TenantId == tenantId)
                    .OrderByDescending(p => p.Version)
                    .FirstOrDefaultAsync(ct);

    public async Task AddAsync(Policy policy, CancellationToken ct = default) =>
        await _db.Policies.AddAsync(policy, ct);
}
