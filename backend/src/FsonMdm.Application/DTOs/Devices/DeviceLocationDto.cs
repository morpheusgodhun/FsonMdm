namespace FsonMdm.Application.DTOs.Devices;

public record DeviceLocationDto(
    double Latitude,
    double Longitude,
    double? Accuracy,
    DateTime CapturedAt);
