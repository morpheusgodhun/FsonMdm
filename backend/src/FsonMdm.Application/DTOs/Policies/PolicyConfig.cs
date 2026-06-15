using System.Text.Json.Serialization;

namespace FsonMdm.Application.DTOs.Policies;

/// <summary>
/// Strongly-typed view of <c>Policy.ConfigJson</c>. Property names map to the
/// JSON contract consumed by the Android agent's policy engine.
/// </summary>
public class PolicyConfig
{
    [JsonPropertyName("kioskMode")]
    public bool KioskMode { get; set; }

    [JsonPropertyName("blockCamera")]
    public bool BlockCamera { get; set; }

    [JsonPropertyName("blockSettings")]
    public bool BlockSettings { get; set; }

    [JsonPropertyName("blockPlayStore")]
    public bool BlockPlayStore { get; set; }

    [JsonPropertyName("allowedApps")]
    public List<string> AllowedApps { get; set; } = new();
}
