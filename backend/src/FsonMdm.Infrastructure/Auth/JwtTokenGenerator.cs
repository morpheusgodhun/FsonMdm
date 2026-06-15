using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FsonMdm.Application.Interfaces.Services;
using FsonMdm.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FsonMdm.Infrastructure.Auth;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public (string Token, DateTime ExpiresAt) GenerateAdminToken(AdminUser user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_settings.AdminTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, AppRoles.Admin),
            new(AppClaimTypes.TenantId, user.TenantId.ToString())
        };
        return (BuildToken(claims, expires), expires);
    }

    public (string Token, DateTime ExpiresAt) GenerateDeviceToken(Device device)
    {
        var expires = DateTime.UtcNow.AddDays(_settings.DeviceTokenDays);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, device.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, AppRoles.Device),
            new(AppClaimTypes.TenantId, device.TenantId.ToString()),
            new(AppClaimTypes.DeviceId, device.Id.ToString()),
            new(AppClaimTypes.DeviceIdentifier, device.DeviceId)
        };
        return (BuildToken(claims, expires), expires);
    }

    private string BuildToken(IEnumerable<Claim> claims, DateTime expires)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
