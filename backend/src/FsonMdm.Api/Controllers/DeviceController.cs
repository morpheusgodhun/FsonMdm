using FsonMdm.Api.Services;
using FsonMdm.Application.DTOs.Devices;
using FsonMdm.Application.Interfaces.Services;
using FsonMdm.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FsonMdm.Api.Controllers;

[ApiController]
[Route("api/device")]
public class DeviceController : ControllerBase
{
    private const string EnrollmentHeader = "X-Enrollment-Token";

    private readonly IDeviceService _deviceService;
    private readonly FileStorage _files;

    public DeviceController(IDeviceService deviceService, FileStorage files)
    {
        _deviceService = deviceService;
        _files = files;
    }

    /// <summary>
    /// Agent registration. Authenticated by the tenant enrollment key in the
    /// <c>X-Enrollment-Token</c> header. Returns a long-lived device JWT.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<RegisterDeviceResponse>> Register(
        [FromBody] RegisterDeviceRequest request, CancellationToken ct)
    {
        var enrollmentKey = Request.Headers[EnrollmentHeader].ToString();
        return Ok(await _deviceService.RegisterAsync(enrollmentKey, request, ct));
    }

    /// <summary>Agent liveness ping. Updates LastSeen / IP.</summary>
    [HttpPost("heartbeat")]
    [Authorize(Roles = AppRoles.Device)]
    public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest request, CancellationToken ct)
    {
        await _deviceService.HeartbeatAsync(request, ct);
        return NoContent();
    }

    /// <summary>Admin: list all devices in the tenant.</summary>
    [HttpGet("list")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<IReadOnlyList<DeviceDto>>> List(CancellationToken ct)
        => Ok(await _deviceService.ListAsync(ct));

    // ---- Location tracking ----

    /// <summary>Agent: report a location fix.</summary>
    [HttpPost("location")]
    [Authorize(Roles = AppRoles.Device)]
    public async Task<IActionResult> ReportLocation([FromBody] LocationReportRequest request, CancellationToken ct)
    {
        await _deviceService.ReportLocationAsync(request, ct);
        return NoContent();
    }

    /// <summary>Admin: location history for a device.</summary>
    [HttpGet("{deviceId:guid}/locations")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<IReadOnlyList<DeviceLocationDto>>> Locations(
        Guid deviceId, [FromQuery] int take = 50, CancellationToken ct = default)
        => Ok(await _deviceService.GetLocationsAsync(deviceId, take, ct));

    // ---- Installed-app inventory ----

    /// <summary>Agent: report installed apps for the kiosk whitelist picker.</summary>
    [HttpPost("apps")]
    [Authorize(Roles = AppRoles.Device)]
    public async Task<IActionResult> ReportApps([FromBody] ReportAppsRequest request, CancellationToken ct)
    {
        await _deviceService.ReportAppsAsync(request, ct);
        return NoContent();
    }

    /// <summary>Admin: installed apps for a device.</summary>
    [HttpGet("{deviceId:guid}/apps")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<IReadOnlyList<DeviceAppDto>>> Apps(Guid deviceId, CancellationToken ct)
        => Ok(await _deviceService.GetAppsAsync(deviceId, ct));

    // ---- Remote screenshot ----

    /// <summary>Agent: upload a captured screenshot (multipart, field name 'file').</summary>
    [HttpPost("screenshot")]
    [Authorize(Roles = AppRoles.Device)]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> UploadScreenshot(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Ekran görüntüsü dosyası boş." });

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".png";
        var storedName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_files.ScreenshotDirectory, storedName);

        await using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream, ct);

        var relativeUrl = FileStorage.ScreenshotRelativeUrl(storedName);
        await _deviceService.SetScreenshotAsync(relativeUrl, ct);
        return Ok(new { url = relativeUrl });
    }
}
