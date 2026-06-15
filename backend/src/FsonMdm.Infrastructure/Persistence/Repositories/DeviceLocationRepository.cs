using FsonMdm.Application.Interfaces.Repositories;
using FsonMdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FsonMdm.Infrastructure.Persistence.Repositories;

public class DeviceLocationRepository : IDeviceLocationRepository
{
    private readonly AppDbContext _db;
    public DeviceLocationRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(DeviceLocation location, CancellationToken ct = default) =>
        await _db.DeviceLocations.AddAsync(location, ct);

    public async Task<IReadOnlyList<DeviceLocation>> ListByDeviceAsync(
        Guid tenantId, Guid deviceId, int take, CancellationToken ct = default) =>
        await _db.DeviceLocations.AsNoTracking()
                 .Where(l => l.TenantId == tenantId && l.DeviceId == deviceId)
                 .OrderByDescending(l => l.CapturedAt)
                 .Take(take <= 0 ? 50 : take)
                 .ToListAsync(ct);
}
