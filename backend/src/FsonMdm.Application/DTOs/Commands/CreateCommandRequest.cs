namespace FsonMdm.Application.DTOs.Commands;

/// <summary>DeviceId is the hardware identifier; Type is LOCK | MESSAGE | RESTART.</summary>
public record CreateCommandRequest(string DeviceId, string Type, string? Payload);
