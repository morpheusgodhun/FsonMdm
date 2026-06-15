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
    public DeviceController(IDeviceService deviceService) => _deviceService = deviceService;

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
}
