using System.Text.Json.Serialization;
using StarModsManager.Api.NexusMods.Interface;

namespace StarModsManager.Api.NexusMods.Responses;

/// <summary>A mod endorsement by the user.</summary>
public sealed class UserEndorsement
{
    /// <summary>The unique mod ID.</summary>
    [JsonPropertyName("mod_id")]
    public int ModId { get; set; }

    /// <summary>The mod's game key.</summary>
    [JsonPropertyName("domain_name")]
    public string DomainName { get; set; } = null!;

    /// <summary>When the user endorsed the mod.</summary>
    [JsonPropertyName("date")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTimeOffset Date { get; set; }

    /// <summary>The current mod version when the user endorsed the mod.</summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = null!;

    /// <summary>The mod endorsement status.</summary>
    [JsonPropertyName("status")]
    public EndorsementStatus Status { get; set; }
}