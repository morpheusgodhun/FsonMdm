using FsonMdm.Application.DTOs.Devices;

namespace FsonMdm.Application.Interfaces.Services;

public interface IDeviceService
{
    Task<RegisterDeviceResponse> RegisterAsync(string enrollmentKey, RegisterDeviceRequest request, CancellationToken ct = default);
    Task HeartbeatAsync(HeartbeatRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<DeviceDto>> ListAsync(CancellationToken ct = default);
}
