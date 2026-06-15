using FsonMdm.Domain.Entities;

namespace FsonMdm.Application.Interfaces.Services;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateAdminToken(AdminUser user);
    (string Token, DateTime ExpiresAt) GenerateDeviceToken(Device device);
}
