using FsonMdm.Domain.Common;

namespace FsonMdm.Domain.Entities;

/// <summary>
/// An APK uploaded by an admin and stored server-side, deployable to devices
/// via an InstallApk command.
/// </summary>
public class ManagedApp : BaseEntity
{
    public Guid TenantId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string AppLabel { get; set; } = string.Empty;
    public string? VersionName { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
}
