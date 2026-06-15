using FsonMdm.Domain.Entities;

namespace FsonMdm.Application.Interfaces.Repositories;

public interface IDeviceAppRepository
{
    Task<IReadOnlyList<DeviceApp>> ListByDeviceAsync(Guid tenantId, Guid deviceId, CancellationToken ct = default);
    Task<IReadOnlyList<DeviceApp>> ListDistinctByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task ReplaceForDeviceAsync(Guid tenantId, Guid deviceId, IEnumerable<DeviceApp> apps, CancellationToken ct = default);
}
