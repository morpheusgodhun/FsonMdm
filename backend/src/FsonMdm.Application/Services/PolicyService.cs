using System.Text.Json;
using FsonMdm.Application.Common.Exceptions;
using FsonMdm.Application.DTOs.Policies;
using FsonMdm.Application.Interfaces.Repositories;
using FsonMdm.Application.Interfaces.Services;
using FsonMdm.Domain.Entities;

namespace FsonMdm.Application.Services;

public class PolicyService : IPolicyService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IPolicyRepository _policies;
    private readonly IDeviceRepository _devices;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public PolicyService(
        IPolicyRepository policies,
        IDeviceRepository devices,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _policies = policies;
        _devices = devices;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<PolicyDto> GetForDeviceAsync(string deviceId, CancellationToken ct = default)
    {
        var device = await _devices.GetByDeviceIdAsync(_tenantContext.TenantId, deviceId, ct)
                     ?? throw new NotFoundException("Cihaz bulunamadı.");

        // Device tokens may only read their own policy.
        if (_tenantContext.DeviceId is { } callerDeviceId && callerDeviceId != device.Id)
            throw new AuthException("Bu cihazın politikasına erişim yetkiniz yok.");

        var policy = await _policies.GetLatestByTenantAsync(_tenantContext.TenantId, ct)
                     ?? throw new NotFoundException("Bu tenant için tanımlı politika yok.");

        return Map(policy);
    }

    public async Task<PolicyDto> UpdateAsync(UpdatePolicyRequest request, CancellationToken ct = default)
    {
        var existing = await _policies.GetLatestByTenantAsync(_tenantContext.TenantId, ct);
        var configJson = JsonSerializer.Serialize(request.Config, JsonOptions);

        if (existing is null)
        {
            existing = new Policy
            {
                TenantId = _tenantContext.TenantId,
                Name = request.Name,
                Version = 1,
                ConfigJson = configJson,
                UpdatedAt = DateTime.UtcNow
            };
            await _policies.AddAsync(existing, ct);
        }
        else
        {
            existing.Name = request.Name;
            existing.ConfigJson = configJson;
            existing.Version += 1;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Map(existing);
    }

    public async Task<PolicyDto?> GetCurrentAsync(CancellationToken ct = default)
    {
        var policy = await _policies.GetLatestByTenantAsync(_tenantContext.TenantId, ct);
        return policy is null ? null : Map(policy);
    }

    private static PolicyDto Map(Policy p)
    {
        var config = string.IsNullOrWhiteSpace(p.ConfigJson)
            ? new PolicyConfig()
            : JsonSerializer.Deserialize<PolicyConfig>(p.ConfigJson, JsonOptions) ?? new PolicyConfig();

        return new PolicyDto(p.Id, p.Name, p.Version, config, p.UpdatedAt);
    }
}
