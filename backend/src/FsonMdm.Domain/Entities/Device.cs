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

    public Tenant Tenant { get; set; } = null!;
    public ICollection<Command> Commands { get; set; } = new List<Command>();
}
