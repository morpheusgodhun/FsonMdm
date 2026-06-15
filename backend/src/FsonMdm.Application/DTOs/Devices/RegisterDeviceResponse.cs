namespace FsonMdm.Application.DTOs.Devices;

/// <summary>Returns a device-scoped JWT the agent uses for all subsequent calls.</summary>
public record RegisterDeviceResponse(Guid Id, string DeviceId, string Token, DateTime ExpiresAt);
