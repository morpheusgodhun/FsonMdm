namespace FsonMdm.Application.DTOs.Commands;

/// <summary>Agent acknowledges a command. Status should be SENT or DONE.</summary>
public record AckCommandRequest(Guid CommandId, string Status);
