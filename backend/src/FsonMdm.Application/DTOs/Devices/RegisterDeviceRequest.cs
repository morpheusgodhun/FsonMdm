namespace FsonMdm.Application.DTOs.Devices;

/// <summary>Sent by the agent on first launch. EnrollmentKey identifies the tenant.</summary>
public record RegisterDeviceRequest(string DeviceId, string Model, string? IPAddress);
