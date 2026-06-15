using FsonMdm.Application.Common.Exceptions;
using FsonMdm.Application.DTOs.Apps;
using FsonMdm.Application.Interfaces.Repositories;
using FsonMdm.Application.Interfaces.Services;
using FsonMdm.Domain.Entities;

namespace FsonMdm.Application.Services;

public class AppCatalogService : IAppCatalogService
{
    private readonly IManagedAppRepository _apps;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public AppCatalogService(
        IManagedAppRepository apps,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _apps = apps;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<ManagedAppDto> CreateAsync(CreateManagedAppRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.StoredFileName))
            throw new ValidationException("Yüklenen dosya adı eksik.");

        var app = new ManagedApp
        {
            TenantId = _tenantContext.TenantId,
            PackageName = string.IsNullOrWhiteSpace(request.PackageName) ? "unknown.package" : request.PackageName.Trim(),
            AppLabel = string.IsNullOrWhiteSpace(request.AppLabel) ? request.FileName : request.AppLabel.Trim(),
            VersionName = request.VersionName,
            FileName = request.FileName,
            StoredFileName = request.StoredFileName,
            FileSizeBytes = request.FileSizeBytes,
            UploadedAt = DateTime.UtcNow
        };

        await _apps.AddAsync(app, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(app);
    }

    public async Task<IReadOnlyList<ManagedAppDto>> ListAsync(CancellationToken ct = default)
    {
        var rows = await _apps.ListByTenantAsync(_tenantContext.TenantId, ct);
        return rows.Select(Map).ToList();
    }

    public async Task<ManagedAppDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var app = await _apps.GetByIdAsync(_tenantContext.TenantId, id, ct);
        return app is null ? null : Map(app);
    }

    public async Task<(string StoredFileName, string FileName)?> GetFileInfoAsync(Guid id, CancellationToken ct = default)
    {
        var app = await _apps.GetByIdAsync(_tenantContext.TenantId, id, ct);
        return app is null ? null : (app.StoredFileName, app.FileName);
    }

    private static ManagedAppDto Map(ManagedApp a) => new(
        a.Id, a.PackageName, a.AppLabel, a.VersionName, a.FileName, a.FileSizeBytes, a.UploadedAt);
}
