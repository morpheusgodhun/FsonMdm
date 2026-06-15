namespace FsonMdm.Domain.Common;

/// <summary>
/// Base type for all persisted entities. Every entity carries a GUID identity
/// and a creation timestamp (UTC).
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
