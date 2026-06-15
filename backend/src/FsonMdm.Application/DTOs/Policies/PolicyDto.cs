namespace FsonMdm.Application.DTOs.Policies;

/// <summary>Returned to the agent. <c>Config</c> is the parsed policy contract.</summary>
public record PolicyDto(
    Guid Id,
    string Name,
    int Version,
    PolicyConfig Config,
    DateTime UpdatedAt);
