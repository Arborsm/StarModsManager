using System.Text.Json.Serialization;
// ReSharper disable InconsistentNaming

namespace StarModsManager.Api.NexusMods.Responses;

/// <summary>A mod tracked by the user.</summary>
public sealed class TrackedMod
{
    /// <summary>The unique mod ID.</summary>
    [JsonPropertyName("mod_id")]
    public int ModID { get; set; }

    /// <summary>The mod's game key.</summary>
    [JsonPropertyName("domain_name")]
    public string DomainName { get; set; } = null!;
}