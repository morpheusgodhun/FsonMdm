using FsonMdm.Application.DTOs.Commands;

namespace FsonMdm.Application.Interfaces.Services;

public interface ICommandService
{
    Task<CommandDto> CreateAsync(CreateCommandRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<CommandDto>> GetPendingAsync(string deviceId, CancellationToken ct = default);
    Task AckAsync(AckCommandRequest request, CancellationToken ct = default);
}
