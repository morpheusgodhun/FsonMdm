using FsonMdm.Domain.Common;
using FsonMdm.Domain.Enums;

namespace FsonMdm.Domain.Entities;

/// <summary>
/// A one-time action queued for a specific device. The agent polls pending
/// commands, executes them and acknowledges back, moving the status forward:
/// Pending -> Sent -> Done.
/// </summary>
public class Command : BaseEntity
{
    public Guid TenantId { get; set; }

    /// <summary>FK to <see cref="Device.Id"/> (the internal GUID, not the hardware id).</summary>
    public Guid DeviceId { get; set; }

    public CommandType Type { get; set; }
    public string? Payload { get; set; }
    public CommandStatus Status { get; set; } = CommandStatus.Pending;

    public Device Device { get; set; } = null!;
}
