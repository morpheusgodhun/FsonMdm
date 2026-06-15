using FsonMdm.Domain.Common;

namespace FsonMdm.Domain.Entities;

/// <summary>
/// Logical isolation boundary. Every other entity is scoped to a Tenant.
/// The <see cref="EnrollmentKey"/> is the shared secret an agent presents on
/// the (single) register call to prove it belongs to this tenant.
/// </summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string EnrollmentKey { get; set; } = string.Empty;

    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<Policy> Policies { get; set; } = new List<Policy>();
    public ICollection<AdminUser> AdminUsers { get; set; } = new List<AdminUser>();
}
