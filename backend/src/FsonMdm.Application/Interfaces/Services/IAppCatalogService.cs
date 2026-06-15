using FsonMdm.Application.DTOs.Apps;

namespace FsonMdm.Application.Interfaces.Services;

public interface IAppCatalogService
{
    Task<ManagedAppDto> CreateAsync(CreateManagedAppRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ManagedAppDto>> ListAsync(CancellationToken ct = default);
    Task<ManagedAppDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(string StoredFileName, string FileName)?> GetFileInfoAsync(Guid id, CancellationToken ct = default);
}
