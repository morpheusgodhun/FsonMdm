namespace FsonMdm.Application.DTOs.Apps;

/// <summary>
/// Created by the API after the uploaded APK file has been persisted to disk.
/// <c>StoredFileName</c> is the server-side file name; <c>FileName</c> is the
/// original client file name.
/// </summary>
public record CreateManagedAppRequest(
    string PackageName,
    string AppLabel,
    string? VersionName,
    string FileName,
    string StoredFileName,
    long FileSizeBytes);
