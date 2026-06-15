using System.Security.Claims;
using FsonMdm.Api.Dashboard.Models;
using FsonMdm.Api.Services;
using FsonMdm.Application.Common.Exceptions;
using FsonMdm.Application.DTOs.Apps;
using FsonMdm.Application.DTOs.Auth;
using FsonMdm.Application.DTOs.Commands;
using FsonMdm.Application.DTOs.Policies;
using FsonMdm.Application.Interfaces.Services;
using FsonMdm.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FsonMdm.Api.Dashboard;

/// <summary>
/// Server-rendered admin dashboard. Authenticates with the cookie scheme; the
/// signed-in principal carries the same tenant/role claims a JWT would, so all
/// Application services run unchanged in-process.
/// </summary>
[Authorize(AuthenticationSchemes = AuthSchemes.Dashboard, Roles = AppRoles.Admin)]
public class DashboardController : Controller
{
    private readonly IAuthService _auth;
    private readonly IDeviceService _devices;
    private readonly IPolicyService _policies;
    private readonly ICommandService _commands;
    private readonly IAppCatalogService _apps;
    private readonly FileStorage _files;

    public DashboardController(
        IAuthService auth,
        IDeviceService devices,
        IPolicyService policies,
        ICommandService commands,
        IAppCatalogService apps,
        FileStorage files)
    {
        _auth = auth;
        _devices = devices;
        _policies = policies;
        _commands = commands;
        _apps = apps;
        _files = files;
    }

    // ---- Auth ----

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(Index));
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        try
        {
            var result = await _auth.LoginAsync(new LoginRequest(model.Username, model.Password), ct);

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, result.Username),
                new(ClaimTypes.Role, AppRoles.Admin),
                new(AppClaimTypes.TenantId, result.TenantId.ToString())
            };
            var identity = new ClaimsIdentity(claims, AuthSchemes.Dashboard);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(AuthSchemes.Dashboard, principal,
                new AuthenticationProperties { IsPersistent = true });

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);
            return RedirectToAction(nameof(Index));
        }
        catch (AuthException ex)
        {
            model.Error = ex.Message;
            model.Password = string.Empty;
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthSchemes.Dashboard);
        return RedirectToAction(nameof(Login));
    }

    // ---- Devices ----

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var devices = await _devices.ListAsync(ct);
        return View(devices);
    }

    [HttpGet]
    public async Task<IActionResult> Device(Guid id, CancellationToken ct)
    {
        var device = await _devices.GetByIdAsync(id, ct);
        var vm = new DeviceDetailViewModel
        {
            Device = device,
            Locations = await _devices.GetLocationsAsync(id, 50, ct),
            Apps = await _devices.GetAppsAsync(id, ct),
            ManagedApps = await _apps.ListAsync(ct),
            StatusMessage = TempData["StatusMessage"] as string
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendCommand(Guid id, string deviceIdentifier, string type, string? payload, CancellationToken ct)
    {
        await _commands.CreateAsync(new CreateCommandRequest(deviceIdentifier, type, payload), ct);
        TempData["StatusMessage"] = $"Komut kuyruğa alındı: {type}";
        return RedirectToAction(nameof(Device), new { id });
    }

    // ---- Policy / kiosk whitelist ----

    [HttpGet]
    public async Task<IActionResult> Policy(CancellationToken ct)
    {
        var current = await _policies.GetCurrentAsync(ct);
        var catalog = await _devices.GetTenantAppCatalogAsync(ct);

        var vm = new PolicyEditViewModel { Catalog = catalog, StatusMessage = TempData["StatusMessage"] as string };
        if (current is not null)
        {
            vm.Name = current.Name;
            vm.Version = current.Version;
            vm.KioskMode = current.Config.KioskMode;
            vm.BlockCamera = current.Config.BlockCamera;
            vm.BlockSettings = current.Config.BlockSettings;
            vm.BlockPlayStore = current.Config.BlockPlayStore;
            vm.AllowedApps = current.Config.AllowedApps ?? new();
        }
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePolicy(
        string name,
        bool kioskMode,
        bool blockCamera,
        bool blockSettings,
        bool blockPlayStore,
        List<string>? allowedApps,
        string? manualApps,
        CancellationToken ct = default)
    {
        var apps = new List<string>(allowedApps ?? new());

        // Allow free-text packages too (newline/comma separated).
        if (!string.IsNullOrWhiteSpace(manualApps))
        {
            var extra = manualApps
                .Split(new[] { '\n', '\r', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            apps.AddRange(extra);
        }

        var config = new PolicyConfig
        {
            KioskMode = kioskMode,
            BlockCamera = blockCamera,
            BlockSettings = blockSettings,
            BlockPlayStore = blockPlayStore,
            AllowedApps = apps.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };

        await _policies.UpdateAsync(
            new UpdatePolicyRequest(string.IsNullOrWhiteSpace(name) ? "Varsayılan Politika" : name, config), ct);

        TempData["StatusMessage"] = "Politika güncellendi. Cihazlar bir sonraki senkronda alacak.";
        return RedirectToAction(nameof(Policy));
    }

    // ---- APK catalog ----

    [HttpGet]
    public async Task<IActionResult> Apps(CancellationToken ct)
    {
        var vm = new AppsViewModel
        {
            Apps = await _apps.ListAsync(ct),
            Devices = await _devices.ListAsync(ct),
            StatusMessage = TempData["StatusMessage"] as string
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(512_000_000)]
    public async Task<IActionResult> UploadApk(
        IFormFile? file, string? packageName, string? appLabel, string? versionName, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            TempData["StatusMessage"] = "Lütfen bir APK dosyası seçin.";
            return RedirectToAction(nameof(Apps));
        }

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".apk";
        var storedName = $"{Guid.NewGuid():N}{ext}";

        await using (var stream = System.IO.File.Create(_files.ApkPath(storedName)))
            await file.CopyToAsync(stream, ct);

        await _apps.CreateAsync(new CreateManagedAppRequest(
            PackageName: packageName ?? string.Empty,
            AppLabel: string.IsNullOrWhiteSpace(appLabel) ? Path.GetFileNameWithoutExtension(file.FileName) : appLabel,
            VersionName: versionName,
            FileName: file.FileName,
            StoredFileName: storedName,
            FileSizeBytes: file.Length), ct);

        TempData["StatusMessage"] = $"APK yüklendi: {file.FileName}";
        return RedirectToAction(nameof(Apps));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PushApk(Guid appId, string deviceIdentifier, CancellationToken ct)
    {
        // Payload is the managed-app id; the agent downloads it via /api/app/download/{id}.
        await _commands.CreateAsync(
            new CreateCommandRequest(deviceIdentifier, "INSTALL_APK", appId.ToString()), ct);

        TempData["StatusMessage"] = "APK kurulum komutu kuyruğa alındı.";
        return RedirectToAction(nameof(Apps));
    }
}
