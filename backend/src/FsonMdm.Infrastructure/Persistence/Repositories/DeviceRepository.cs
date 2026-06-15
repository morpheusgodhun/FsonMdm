using FsonMdm.Application.Interfaces.Repositories;
using FsonMdm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FsonMdm.Infrastructure.Persistence.Repositories;

public class DeviceRepository : IDeviceRepository
{
    private readonly AppDbContext _db;
    public DeviceRepository(AppDbContext db) => _db = db;

    public Task<Device?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default) =>
        _db.Devices.FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == id, ct);

    public Task<Device?> GetByDeviceIdAsync(Guid tenantId, string deviceId, CancellationToken ct = default) =>
        _db.Devices.FirstOrDefaultAsync(d => d.TenantId == tenantId && d.DeviceId == deviceId, ct);

    public async Task<IReadOnlyList<Device>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        await _db.Devices.AsNoTracking()
                 .Where(d => d.TenantId == tenantId)
                 .OrderByDescending(d => d.LastSeen)
                 .ToListAsync(ct);

    public async Task AddAsync(Device device, CancellationToken ct = default) =>
        await _db.Devices.AddAsync(device, ct);
}
