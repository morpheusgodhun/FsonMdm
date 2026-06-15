using FsonMdm.Application.DTOs.Devices;

namespace FsonMdm.Application.Interfaces.Services;

public interface IDeviceService
{
    Task<RegisterDeviceResponse> RegisterAsync(string enrollmentKey, RegisterDeviceRequest request, CancellationToken ct = default);
    Task HeartbeatAsync(HeartbeatRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<DeviceDto>> ListAsync(CancellationToken ct = default);

    // Admin: single device for the dashboard detail view.
    Task<DeviceDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Location tracking.
    Task ReportLocationAsync(LocationReportRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<DeviceLocationDto>> GetLocationsAsync(Guid deviceId, int take = 50, CancellationToken ct = default);

    // Installed-app inventory.
    Task ReportAppsAsync(ReportAppsRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<DeviceAppDto>> GetAppsAsync(Guid deviceId, CancellationToken ct = default);
    Task<IReadOnlyList<DeviceAppDto>> GetTenantAppCatalogAsync(CancellationToken ct = default);

    // Remote screenshot: agent stores latest image path for the calling device.
    Task SetScreenshotAsync(string relativePath, CancellationToken ct = default);
}
