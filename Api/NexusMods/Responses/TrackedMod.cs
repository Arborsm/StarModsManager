// ReSharper disable InconsistentNaming

using Newtonsoft.Json;

namespace StarModsManager.Api.NexusMods.Responses;

/// <summary>A mod tracked by the user.</summary>
public sealed class TrackedMod
{
    /// <summary>The unique mod ID.</summary>
    [JsonProperty("mod_id")]
    public int ModID { get; set; }

    /// <summary>The mod's game key.</summary>
    [JsonProperty("domain_name")]
    public string DomainName { get; set; } = null!;
}