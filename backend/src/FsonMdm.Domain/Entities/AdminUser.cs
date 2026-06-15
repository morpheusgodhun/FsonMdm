using FsonMdm.Domain.Common;

namespace FsonMdm.Domain.Entities;

/// <summary>
/// A back-office operator that authenticates with username/password and
/// manages the devices/policies/commands of a single tenant.
/// </summary>
public class AdminUser : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public Tenant Tenant { get; set; } = null!;
}
