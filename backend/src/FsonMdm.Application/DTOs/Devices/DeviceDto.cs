namespace FsonMdm.Application.DTOs.Devices;

public record DeviceDto(
    Guid Id,
    string DeviceId,
    string Model,
    string? IPAddress,
    string Status,
    DateTime? LastSeen,
    DateTime CreatedAt,
    double? LastLatitude,
    double? LastLongitude,
    DateTime? LastLocationAt,
    string? LastScreenshotPath,
    DateTime? LastScreenshotAt);
