namespace FsonMdm.Application.DTOs.Auth;

public record LoginResponse(string Token, DateTime ExpiresAt, Guid TenantId, string Username);
