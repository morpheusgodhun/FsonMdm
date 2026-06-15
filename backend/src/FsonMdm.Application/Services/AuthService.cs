using FsonMdm.Application.Common.Exceptions;
using FsonMdm.Application.Common.Security;
using FsonMdm.Application.DTOs.Auth;
using FsonMdm.Application.Interfaces.Repositories;
using FsonMdm.Application.Interfaces.Services;

namespace FsonMdm.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAdminUserRepository _adminUsers;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public AuthService(IAdminUserRepository adminUsers, IJwtTokenGenerator tokenGenerator)
    {
        _adminUsers = adminUsers;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _adminUsers.GetByUsernameAsync(request.Username, ct);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            throw new AuthException("Kullanıcı adı veya parola hatalı.");

        var (token, expiresAt) = _tokenGenerator.GenerateAdminToken(user);
        return new LoginResponse(token, expiresAt, user.TenantId, user.Username);
    }
}
