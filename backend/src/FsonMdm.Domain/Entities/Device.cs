using FsonMdm.Domain.Common;
using FsonMdm.Domain.Enums;

namespace FsonMdm.Domain.Entities;

/// <summary>
/// A managed Android device (Device Owner already granted on the handset).
/// <see cref="DeviceId"/> is the stable hardware identifier reported by the agent,
/// unique within a tenant.
/// </summary>
public class Device : BaseEntity
{
    public Guid TenantId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? IPAddress { get; set; }
    public DeviceStatus Status { get; set; } = DeviceStatus.Inactive;
    public DateTime? LastSeen { get; set; }

    // Last known location (denormalised for quick listing/map display).
    public double? LastLatitude { get; set; }
    public double? LastLongitude { get; set; }
    public DateTime? LastLocationAt { get; set; }

    // Latest remote screenshot (relative URL under the uploads path).
    public string? LastScreenshotPath { get; set; }
    public DateTime? LastScreenshotAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<Command> Commands { get; set; } = new List<Command>();
    public ICollection<DeviceLocation> Locations { get; set; } = new List<DeviceLocation>();
    public ICollection<DeviceApp> Apps { get; set; } = new List<DeviceApp>();
}
