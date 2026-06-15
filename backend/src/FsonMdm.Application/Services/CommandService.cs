using FsonMdm.Application.Common.Exceptions;
using FsonMdm.Application.DTOs.Commands;
using FsonMdm.Application.Interfaces.Repositories;
using FsonMdm.Application.Interfaces.Services;
using FsonMdm.Domain.Entities;
using FsonMdm.Domain.Enums;

namespace FsonMdm.Application.Services;

public class CommandService : ICommandService
{
    private readonly ICommandRepository _commands;
    private readonly IDeviceRepository _devices;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CommandService(
        ICommandRepository commands,
        IDeviceRepository devices,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _commands = commands;
        _devices = devices;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<CommandDto> CreateAsync(CreateCommandRequest request, CancellationToken ct = default)
    {
        var type = ParseCommandType(request.Type);

        var device = await _devices.GetByDeviceIdAsync(_tenantContext.TenantId, request.DeviceId, ct)
                     ?? throw new NotFoundException("Cihaz bulunamadı.");

        var command = new Command
        {
            TenantId = _tenantContext.TenantId,
            DeviceId = device.Id,
            Type = type,
            Payload = request.Payload,
            Status = CommandStatus.Pending
        };

        await _commands.AddAsync(command, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Map(command);
    }

    public async Task<IReadOnlyList<CommandDto>> GetPendingAsync(string deviceId, CancellationToken ct = default)
    {
        var device = await _devices.GetByDeviceIdAsync(_tenantContext.TenantId, deviceId, ct)
                     ?? throw new NotFoundException("Cihaz bulunamadı.");

        if (_tenantContext.DeviceId is { } callerDeviceId && callerDeviceId != device.Id)
            throw new AuthException("Bu cihazın komutlarına erişim yetkiniz yok.");

        var pending = await _commands.GetPendingByDeviceAsync(_tenantContext.TenantId, device.Id, ct);
        return pending.Select(Map).ToList();
    }

    public async Task AckAsync(AckCommandRequest request, CancellationToken ct = default)
    {
        var status = ParseCommandStatus(request.Status);

        var command = await _commands.GetByIdAsync(_tenantContext.TenantId, request.CommandId, ct)
                      ?? throw new NotFoundException("Komut bulunamadı.");

        if (_tenantContext.DeviceId is { } callerDeviceId && callerDeviceId != command.DeviceId)
            throw new AuthException("Bu komutu onaylama yetkiniz yok.");

        command.Status = status;
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static CommandType ParseCommandType(string value) =>
        value?.Trim().ToUpperInvariant() switch
        {
            "LOCK" => CommandType.Lock,
            "MESSAGE" => CommandType.Message,
            "RESTART" => CommandType.Restart,
            "INSTALL_APK" => CommandType.InstallApk,
            "REQUEST_LOCATION" => CommandType.RequestLocation,
            "SCREENSHOT" => CommandType.Screenshot,
            _ => throw new ValidationException(
                $"Geçersiz komut tipi: '{value}'. (LOCK | MESSAGE | RESTART | INSTALL_APK | REQUEST_LOCATION | SCREENSHOT)")
        };

    private static CommandStatus ParseCommandStatus(string value) =>
        value?.Trim().ToUpperInvariant() switch
        {
            "PENDING" => CommandStatus.Pending,
            "SENT" => CommandStatus.Sent,
            "DONE" => CommandStatus.Done,
            _ => throw new ValidationException($"Geçersiz komut durumu: '{value}'. (PENDING | SENT | DONE)")
        };

    private static CommandDto Map(Command c) =>
        new(c.Id, ToWire(c.Type), c.Payload, c.Status.ToString().ToUpperInvariant(), c.CreatedAt);

    /// <summary>Stable wire representation matching the accepted input tokens.</summary>
    private static string ToWire(CommandType type) => type switch
    {
        CommandType.Lock => "LOCK",
        CommandType.Message => "MESSAGE",
        CommandType.Restart => "RESTART",
        CommandType.InstallApk => "INSTALL_APK",
        CommandType.RequestLocation => "REQUEST_LOCATION",
        CommandType.Screenshot => "SCREENSHOT",
        _ => type.ToString().ToUpperInvariant()
    };
}
