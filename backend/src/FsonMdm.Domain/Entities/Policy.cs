using FsonMdm.Domain.Common;

namespace FsonMdm.Domain.Entities;

/// <summary>
/// The enforcement state applied to a tenant's devices. Stored as a raw JSON
/// blob (<see cref="ConfigJson"/>) so the schema can evolve without migrations.
/// <see cref="Version"/> is bumped on every update so agents can detect changes.
/// </summary>
public class Policy : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = "Default Policy";
    public int Version { get; set; } = 1;
    public string ConfigJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Tenant Tenant { get; set; } = null!;
}
