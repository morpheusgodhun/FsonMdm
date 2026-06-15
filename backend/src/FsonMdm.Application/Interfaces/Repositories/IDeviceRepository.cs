using FsonMdm.Domain.Entities;

namespace FsonMdm.Application.Interfaces.Repositories;

public interface IDeviceRepository
{
    Task<Device?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<Device?> GetByDeviceIdAsync(Guid tenantId, string deviceId, CancellationToken ct = default);
    Task<IReadOnlyList<Device>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Device device, CancellationToken ct = default);
}
