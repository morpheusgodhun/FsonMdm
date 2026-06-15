using FsonMdm.Domain.Common;

namespace FsonMdm.Domain.Entities;

/// <summary>A single location fix reported by the agent for a device.</summary>
public class DeviceLocation : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid DeviceId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Accuracy { get; set; }
    public DateTime CapturedAt { get; set; }

    public Device Device { get; set; } = null!;
}
