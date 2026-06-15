namespace FsonMdm.Infrastructure.Auth;

public static class AppClaimTypes
{
    public const string TenantId = "tenantId";
    public const string DeviceId = "deviceId";
    public const string DeviceIdentifier = "deviceIdentifier";
}

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Device = "Device";
}

public static class AuthSchemes
{
    /// <summary>Cookie scheme used by the server-rendered admin dashboard.</summary>
    public const string Dashboard = "Dashboard";
}
