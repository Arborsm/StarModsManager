using System.Text.Json.Serialization;
using StarModsManager.Api.NexusMods.Interface;

namespace StarModsManager.Api.NexusMods.Responses;

/// <summary>A Nexus mod update record.</summary>
public sealed class ModUpdate
{
    /// <summary>The unique mod ID.</summary>
    [JsonPropertyName("mod_id")]
    public int ModId { get; set; }

    /// <summary>When the mod files were last changed.</summary>
    [JsonPropertyName("latest_file_update")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTimeOffset LatestFileUpdate { get; set; }

    /// <summary>When the mod data was last changed.</summary>
    [JsonPropertyName("latest_mod_activity")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTimeOffset LatestModActivity { get; set; }
}