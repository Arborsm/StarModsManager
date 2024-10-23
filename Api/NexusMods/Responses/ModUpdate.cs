using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace StarModsManager.Api.NexusMods.Responses;

/// <summary>A Nexus mod update record.</summary>
public sealed class ModUpdate
{
    /// <summary>The unique mod ID.</summary>
    [JsonProperty("mod_id")]
    public int ModId { get; set; }

    /// <summary>When the mod files were last changed.</summary>
    [JsonProperty("latest_file_update")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTimeOffset LatestFileUpdate { get; set; }

    /// <summary>When the mod data was last changed.</summary>
    [JsonProperty("latest_mod_activity")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTimeOffset LatestModActivity { get; set; }
}