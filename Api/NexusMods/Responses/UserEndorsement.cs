using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace StarModsManager.Api.NexusMods.Responses;

/// <summary>A mod endorsement by the user.</summary>
public sealed class UserEndorsement
{
    /// <summary>The unique mod ID.</summary>
    [JsonProperty("mod_id")]
    public int ModId { get; set; }

    /// <summary>The mod's game key.</summary>
    [JsonProperty("domain_name")]
    public string DomainName { get; set; } = null!;

    /// <summary>When the user endorsed the mod.</summary>
    [JsonProperty("date")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTimeOffset Date { get; set; }

    /// <summary>The current mod version when the user endorsed the mod.</summary>
    [JsonProperty("version")]
    public string Version { get; set; } = null!;

    /// <summary>The mod endorsement status.</summary>
    [JsonProperty("status")]
    public EndorsementStatus Status { get; set; }
}