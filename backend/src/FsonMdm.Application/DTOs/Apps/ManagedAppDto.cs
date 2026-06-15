namespace FsonMdm.Application.DTOs.Apps;

public record ManagedAppDto(
    Guid Id,
    string PackageName,
    string AppLabel,
    string? VersionName,
    string FileName,
    long FileSizeBytes,
    DateTime UploadedAt);
