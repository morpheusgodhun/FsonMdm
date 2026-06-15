using FsonMdm.Application.Common.Exceptions;
using FsonMdm.Application.DTOs.Devices;
using FsonMdm.Application.Interfaces.Repositories;
using FsonMdm.Application.Interfaces.Services;
using FsonMdm.Domain.Entities;
using FsonMdm.Domain.Enums;

namespace FsonMdm.Application.Services;

public class DeviceService : IDeviceService
{
    private readonly ITenantRepository _tenants;
    private readonly IDeviceRepository _devices;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public DeviceService(
        ITenantRepository tenants,
        IDeviceRepository devices,
        IJwtTokenGenerator tokenGenerator,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _tenants = tenants;
        _devices = devices;
        _tokenGenerator = tokenGenerator;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<RegisterDeviceResponse> RegisterAsync(
        string enrollmentKey, RegisterDeviceRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(enrollmentKey))
            throw new AuthException("Enrollment anahtarı eksik.");
        if (string.IsNullOrWhiteSpace(request.DeviceId))
            throw new ValidationException("DeviceId zorunludur.");

        var tenant = await _tenants.GetByEnrollmentKeyAsync(enrollmentKey, ct)
                     ?? throw new AuthException("Geçersiz enrollment anahtarı.");

        // Re-register is idempotent: an existing device is re-activated, not duplicated.
        var device = await _devices.GetByDeviceIdAsync(tenant.Id, request.DeviceId, ct);
        if (device is null)
        {
            device = new Device
            {
                TenantId = tenant.Id,
                DeviceId = request.DeviceId,
                Model = request.Model,
                IPAddress = request.IPAddress,
                Status = DeviceStatus.Active,
                LastSeen = DateTime.UtcNow
            };
            await _devices.AddAsync(device, ct);
        }
        else
        {
            device.Model = request.Model;
            device.IPAddress = request.IPAddress;
            device.Status = DeviceStatus.Active;
            device.LastSeen = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(ct);

        var (token, expiresAt) = _tokenGenerator.GenerateDeviceToken(device);
        return new RegisterDeviceResponse(device.Id, device.DeviceId, token, expiresAt);
    }

    public async Task HeartbeatAsync(HeartbeatRequest request, CancellationToken ct = default)
    {
        var deviceId = _tenantContext.DeviceId
                       ?? throw new AuthException("Heartbeat yalnızca cihaz tokenı ile çağrılabilir.");

        var device = await _devices.GetByIdAsync(_tenantContext.TenantId, deviceId, ct)
                     ?? throw new NotFoundException("Cihaz bulunamadı.");

        device.LastSeen = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.IPAddress))
            device.IPAddress = request.IPAddress;

        // A blocked device stays blocked; otherwise a live heartbeat means Active.
        if (device.Status != DeviceStatus.Blocked)
            device.Status = DeviceStatus.Active;

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DeviceDto>> ListAsync(CancellationToken ct = default)
    {
        var devices = await _devices.ListByTenantAsync(_tenantContext.TenantId, ct);
        return devices.Select(Map).ToList();
    }

    private static DeviceDto Map(Device d) => new(
        d.Id, d.DeviceId, d.Model, d.IPAddress, d.Status.ToString(), d.LastSeen, d.CreatedAt);
}
