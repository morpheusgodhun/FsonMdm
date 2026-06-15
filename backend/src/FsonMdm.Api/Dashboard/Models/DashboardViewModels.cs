using FsonMdm.Application.DTOs.Apps;
using FsonMdm.Application.DTOs.Devices;
using FsonMdm.Application.DTOs.Policies;

namespace FsonMdm.Api.Dashboard.Models;

public class LoginViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
    public string? Error { get; set; }
}

public class DeviceDetailViewModel
{
    public DeviceDto Device { get; set; } = null!;
    public IReadOnlyList<DeviceLocationDto> Locations { get; set; } = Array.Empty<DeviceLocationDto>();
    public IReadOnlyList<DeviceAppDto> Apps { get; set; } = Array.Empty<DeviceAppDto>();
    public IReadOnlyList<ManagedAppDto> ManagedApps { get; set; } = Array.Empty<ManagedAppDto>();
    public string? StatusMessage { get; set; }
}

public class PolicyEditViewModel
{
    public string Name { get; set; } = "Varsayılan Politika";
    public int Version { get; set; }
    public bool KioskMode { get; set; }
    public bool BlockCamera { get; set; }
    public bool BlockSettings { get; set; }
    public bool BlockPlayStore { get; set; }

    /// <summary>Packages currently in the whitelist.</summary>
    public List<string> AllowedApps { get; set; } = new();

    /// <summary>All apps reported across tenant devices (for the picker).</summary>
    public IReadOnlyList<DeviceAppDto> Catalog { get; set; } = Array.Empty<DeviceAppDto>();

    public string? StatusMessage { get; set; }
}

public class AppsViewModel
{
    public IReadOnlyList<ManagedAppDto> Apps { get; set; } = Array.Empty<ManagedAppDto>();
    public IReadOnlyList<DeviceDto> Devices { get; set; } = Array.Empty<DeviceDto>();
    public string? StatusMessage { get; set; }
}
