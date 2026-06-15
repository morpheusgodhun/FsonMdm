using FsonMdm.Application.DTOs.Auth;

namespace FsonMdm.Application.Interfaces.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
