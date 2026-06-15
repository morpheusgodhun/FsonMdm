namespace FsonMdm.Application.DTOs.Devices;

/// <summary>Agent reports its installed-app inventory for the kiosk whitelist picker.</summary>
public record ReportAppsRequest(List<DeviceAppItem> Apps);

public record DeviceAppItem(string PackageName, string AppLabel, bool IsLaunchable);
