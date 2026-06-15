using FsonMdm.Domain.Entities;

namespace FsonMdm.Application.Interfaces.Repositories;

public interface IDeviceLocationRepository
{
    Task AddAsync(DeviceLocation location, CancellationToken ct = default);
    Task<IReadOnlyList<DeviceLocation>> ListByDeviceAsync(Guid tenantId, Guid deviceId, int take, CancellationToken ct = default);
}
