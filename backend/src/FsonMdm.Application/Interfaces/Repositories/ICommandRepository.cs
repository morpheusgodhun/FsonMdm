using FsonMdm.Domain.Entities;

namespace FsonMdm.Application.Interfaces.Repositories;

public interface ICommandRepository
{
    Task<Command?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Command>> GetPendingByDeviceAsync(Guid tenantId, Guid deviceId, CancellationToken ct = default);
    Task AddAsync(Command command, CancellationToken ct = default);
}
