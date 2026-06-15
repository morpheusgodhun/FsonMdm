using FsonMdm.Domain.Common;

namespace FsonMdm.Domain.Entities;

/// <summary>
/// An app reported as installed on a device. Drives the kiosk whitelist picker
/// in the dashboard. Unique per (DeviceId, PackageName).
/// </summary>
public class DeviceApp : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid DeviceId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string AppLabel { get; set; } = string.Empty;
    public bool IsLaunchable { get; set; }
    public DateTime ReportedAt { get; set; }

    public Device Device { get; set; } = null!;
}
