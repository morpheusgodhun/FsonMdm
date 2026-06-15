using FsonMdm.Application.Interfaces.Repositories;
using FsonMdm.Domain.Entities;
using FsonMdm.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FsonMdm.Infrastructure.Persistence.Repositories;

public class CommandRepository : ICommandRepository
{
    private readonly AppDbContext _db;
    public CommandRepository(AppDbContext db) => _db = db;

    public Task<Command?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default) =>
        _db.Commands.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == id, ct);

    public async Task<IReadOnlyList<Command>> GetPendingByDeviceAsync(Guid tenantId, Guid deviceId, CancellationToken ct = default) =>
        await _db.Commands.AsNoTracking()
                 .Where(c => c.TenantId == tenantId
                             && c.DeviceId == deviceId
                             && c.Status == CommandStatus.Pending)
                 .OrderBy(c => c.CreatedAt)
                 .ToListAsync(ct);

    public async Task AddAsync(Command command, CancellationToken ct = default) =>
        await _db.Commands.AddAsync(command, ct);
}
