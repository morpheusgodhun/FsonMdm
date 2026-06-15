using FsonMdm.Application.Interfaces.Repositories;
using FsonMdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FsonMdm.Infrastructure.Persistence.Repositories;

public class ManagedAppRepository : IManagedAppRepository
{
    private readonly AppDbContext _db;
    public ManagedAppRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(ManagedApp app, CancellationToken ct = default) =>
        await _db.ManagedApps.AddAsync(app, ct);

    public Task<ManagedApp?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default) =>
        _db.ManagedApps.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == id, ct);

    public async Task<IReadOnlyList<ManagedApp>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        await _db.ManagedApps.AsNoTracking()
                 .Where(a => a.TenantId == tenantId)
                 .OrderByDescending(a => a.UploadedAt)
                 .ToListAsync(ct);
}
