using FsonMdm.Application.Interfaces.Repositories;
using FsonMdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FsonMdm.Infrastructure.Persistence.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly AppDbContext _db;
    public TenantRepository(AppDbContext db) => _db = db;

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Tenant?> GetByEnrollmentKeyAsync(string enrollmentKey, CancellationToken ct = default) =>
        _db.Tenants.FirstOrDefaultAsync(t => t.EnrollmentKey == enrollmentKey, ct);
}
