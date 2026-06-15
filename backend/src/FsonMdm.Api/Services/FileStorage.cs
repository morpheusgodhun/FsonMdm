namespace FsonMdm.Api.Services;

/// <summary>
/// Resolves on-disk locations for uploaded artifacts:
/// - APKs live under ContentRoot/App_Data/apks (served via an authorized endpoint).
/// - Screenshots live under wwwroot/uploads/screenshots (served as static files).
/// </summary>
public class FileStorage
{
    private readonly IWebHostEnvironment _env;

    public FileStorage(IWebHostEnvironment env) => _env = env;

    public string ApkDirectory
    {
        get
        {
            var dir = Path.Combine(_env.ContentRootPath, "App_Data", "apks");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public string ScreenshotDirectory
    {
        get
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var dir = Path.Combine(webRoot, "uploads", "screenshots");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public string ApkPath(string storedFileName) => Path.Combine(ApkDirectory, storedFileName);

    /// <summary>Public (static-served) relative URL for a stored screenshot file.</summary>
    public static string ScreenshotRelativeUrl(string storedFileName) => $"/uploads/screenshots/{storedFileName}";
}
