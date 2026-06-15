namespace FsonMdm.Application.DTOs.Policies;

/// <summary>Admin upsert for the tenant policy. Version is managed server-side.</summary>
public record UpdatePolicyRequest(string Name, PolicyConfig Config);
