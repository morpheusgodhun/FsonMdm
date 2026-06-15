namespace FsonMdm.Application.Interfaces.Services;

/// <summary>
/// Ambient information about the authenticated caller, resolved from JWT claims.
/// Both admin and device tokens carry a TenantId; device tokens additionally
/// carry the device identity.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
    Guid? DeviceId { get; }
    string? DeviceIdentifier { get; }
    string? Role { get; }
}
