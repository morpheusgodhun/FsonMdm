using FsonMdm.Application.DTOs.Policies;

namespace FsonMdm.Application.Interfaces.Services;

public interface IPolicyService
{
    Task<PolicyDto> GetForDeviceAsync(string deviceId, CancellationToken ct = default);
    Task<PolicyDto> UpdateAsync(UpdatePolicyRequest request, CancellationToken ct = default);
}
