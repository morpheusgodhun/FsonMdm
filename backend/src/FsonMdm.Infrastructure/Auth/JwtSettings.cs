namespace FsonMdm.Infrastructure.Auth;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "FsonMdm";
    public string Audience { get; set; } = "FsonMdm.Agents";
    public string Key { get; set; } = string.Empty;
    public int AdminTokenMinutes { get; set; } = 480;
    public int DeviceTokenDays { get; set; } = 365;
}
