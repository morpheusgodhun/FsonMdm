namespace FsonMdm.Application.DTOs.Commands;

public record CommandDto(
    Guid Id,
    string Type,
    string? Payload,
    string Status,
    DateTime CreatedAt);
