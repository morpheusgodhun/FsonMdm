using FsonMdm.Api.Services;
using FsonMdm.Application.DTOs.Apps;
using FsonMdm.Application.Interfaces.Services;
using FsonMdm.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace FsonMdm.Api.Controllers;

/// <summary>
/// APK catalog: admins upload APKs; devices download them when an InstallApk
/// command targets them.
/// </summary>
[ApiController]
[Route("api/app")]
public class AppController : ControllerBase
{
    private readonly IAppCatalogService _catalog;
    private readonly FileStorage _files;

    public AppController(IAppCatalogService catalog, FileStorage files)
    {
        _catalog = catalog;
        _files = files;
    }

    /// <summary>Admin: upload an APK (multipart, field name 'file').</summary>
    [HttpPost("upload")]
    [Authorize(Roles = AppRoles.Admin)]
    [RequestSizeLimit(512_000_000)]
    public async Task<ActionResult<ManagedAppDto>> Upload(
        IFormFile file,
        [FromForm] string? packageName,
        [FromForm] string? appLabel,
        [FromForm] string? versionName,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "APK dosyası boş." });

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".apk";
        var storedName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = _files.ApkPath(storedName);

        await using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream, ct);

        var dto = await _catalog.CreateAsync(new CreateManagedAppRequest(
            PackageName: packageName ?? string.Empty,
            AppLabel: appLabel ?? Path.GetFileNameWithoutExtension(file.FileName),
            VersionName: versionName,
            FileName: file.FileName,
            StoredFileName: storedName,
            FileSizeBytes: file.Length), ct);

        return Ok(dto);
    }

    /// <summary>Admin: list uploaded APKs.</summary>
    [HttpGet("list")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<IReadOnlyList<ManagedAppDto>>> List(CancellationToken ct)
        => Ok(await _catalog.ListAsync(ct));

    /// <summary>
    /// Download an APK file. Allowed for both the device agent (to install) and
    /// admins. Tenant ownership is enforced by the catalog service.
    /// </summary>
    [HttpGet("download/{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Device}")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var info = await _catalog.GetFileInfoAsync(id, ct);
        if (info is null) return NotFound();

        var path = _files.ApkPath(info.Value.StoredFileName);
        if (!System.IO.File.Exists(path)) return NotFound();

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(info.Value.FileName, out var contentType))
            contentType = "application/vnd.android.package-archive";

        var stream = System.IO.File.OpenRead(path);
        return File(stream, contentType, info.Value.FileName);
    }
}
