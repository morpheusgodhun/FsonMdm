using System.Security.Claims;
using FsonMdm.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace FsonMdm.Infrastructure.Auth;

/// <summary>
/// Resolves the current caller's tenant/device identity from the validated JWT.
/// Throws when accessed outside an authenticated request, which keeps tenant
/// scoping mandatory rather than silently defaulting to an empty GUID.
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _accessor;

    public TenantContext(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public Guid TenantId
    {
        get
        {
            var raw = User?.FindFirstValue(AppClaimTypes.TenantId);
            return Guid.TryParse(raw, out var id)
                ? id
                : throw new InvalidOperationException("İstek bağlamında geçerli bir TenantId bulunamadı.");
        }
    }

    public Guid? DeviceId
    {
        get
        {
            var raw = User?.FindFirstValue(AppClaimTypes.DeviceId);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? DeviceIdentifier => User?.FindFirstValue(AppClaimTypes.DeviceIdentifier);

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);
}
