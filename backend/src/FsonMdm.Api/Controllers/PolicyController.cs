using FsonMdm.Application.DTOs.Policies;
using FsonMdm.Application.Interfaces.Services;
using FsonMdm.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FsonMdm.Api.Controllers;

[ApiController]
[Route("api/policy")]
[Authorize]
public class PolicyController : ControllerBase
{
    private readonly IPolicyService _policyService;
    public PolicyController(IPolicyService policyService) => _policyService = policyService;

    /// <summary>Agent (or admin) fetches the active policy for a device.</summary>
    [HttpGet("{deviceId}")]
    public async Task<ActionResult<PolicyDto>> Get(string deviceId, CancellationToken ct)
        => Ok(await _policyService.GetForDeviceAsync(deviceId, ct));

    /// <summary>Admin: create or update the tenant policy (version is auto-incremented).</summary>
    [HttpPost("update")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<PolicyDto>> Update([FromBody] UpdatePolicyRequest request, CancellationToken ct)
        => Ok(await _policyService.UpdateAsync(request, ct));
}
