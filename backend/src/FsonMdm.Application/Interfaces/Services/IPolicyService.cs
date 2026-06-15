using FsonMdm.Application.DTOs.Policies;

namespace FsonMdm.Application.Interfaces.Services;

public interface IPolicyService
{
    Task<PolicyDto> GetForDeviceAsync(string deviceId, CancellationToken ct = default);
    Task<PolicyDto> UpdateAsync(UpdatePolicyRequest request, CancellationToken ct = default);

    /// <summary>Current tenant policy for the dashboard editor; null when none exists yet.</summary>
    Task<PolicyDto?> GetCurrentAsync(CancellationToken ct = default);
}
