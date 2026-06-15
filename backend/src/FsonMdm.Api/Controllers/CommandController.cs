using FsonMdm.Application.DTOs.Commands;
using FsonMdm.Application.Interfaces.Services;
using FsonMdm.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FsonMdm.Api.Controllers;

[ApiController]
[Route("api/command")]
[Authorize]
public class CommandController : ControllerBase
{
    private readonly ICommandService _commandService;
    public CommandController(ICommandService commandService) => _commandService = commandService;

    /// <summary>Admin: queue a one-time command (LOCK | MESSAGE | RESTART) for a device.</summary>
    [HttpPost("create")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<CommandDto>> Create([FromBody] CreateCommandRequest request, CancellationToken ct)
        => Ok(await _commandService.CreateAsync(request, ct));

    /// <summary>Agent: poll pending commands for a device.</summary>
    [HttpGet("pending/{deviceId}")]
    public async Task<ActionResult<IReadOnlyList<CommandDto>>> Pending(string deviceId, CancellationToken ct)
        => Ok(await _commandService.GetPendingAsync(deviceId, ct));

    /// <summary>Agent: acknowledge a command after execution (SENT | DONE).</summary>
    [HttpPost("ack")]
    [Authorize(Roles = AppRoles.Device)]
    public async Task<IActionResult> Ack([FromBody] AckCommandRequest request, CancellationToken ct)
    {
        await _commandService.AckAsync(request, ct);
        return NoContent();
    }
}
