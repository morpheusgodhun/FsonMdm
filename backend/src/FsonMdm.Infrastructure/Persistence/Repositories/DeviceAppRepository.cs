using FsonMdm.Application.Interfaces.Repositories;
using FsonMdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FsonMdm.Infrastructure.Persistence.Repositories;

public class DeviceAppRepository : IDeviceAppRepository
{
    private readonly AppDbContext _db;
    public DeviceAppRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<DeviceApp>> ListByDeviceAsync(
        Guid tenantId, Guid deviceId, CancellationToken ct = default) =>
        await _db.DeviceApps.AsNoTracking()
                 .Where(a => a.TenantId == tenantId && a.DeviceId == deviceId)
                 .OrderBy(a => a.AppLabel)
                 .ToListAsync(ct);

    /// <summary>Distinct apps across all tenant devices, for the kiosk whitelist picker.</summary>
    public async Task<IReadOnlyList<DeviceApp>> ListDistinctByTenantAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        var rows = await _db.DeviceApps.AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .ToListAsync(ct);

        return rows
            .GroupBy(a => a.PackageName)
            .Select(g => g.OrderByDescending(x => x.IsLaunchable).First())
            .OrderBy(a => a.AppLabel)
            .ToList();
    }

    /// <summary>Replaces the full inventory for a device (delete + insert).</summary>
    public async Task ReplaceForDeviceAsync(
        Guid tenantId, Guid deviceId, IEnumerable<DeviceApp> apps, CancellationToken ct = default)
    {
        var existing = await _db.DeviceApps
            .Where(a => a.TenantId == tenantId && a.DeviceId == deviceId)
            .ToListAsync(ct);

        _db.DeviceApps.RemoveRange(existing);
        await _db.DeviceApps.AddRangeAsync(apps, ct);
    }
}
