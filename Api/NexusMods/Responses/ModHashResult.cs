using Newtonsoft.Json;

namespace StarModsManager.Api.NexusMods.Responses;

/// <summary>The result of a mod MD5 hash search.</summary>
public sealed class ModHashResult
{
    /// <summary>The matched mod.</summary>
    [JsonProperty("mod")]
    public ModInfo ModInfo { get; set; } = null!;

    /// <summary>The matched file details.</summary>
    [JsonProperty("file_details")]
    public ModFileWithHash FileDetails { get; set; } = null!;
}

/// <summary>A downloadable mod file, with its MD5 hash.</summary>
public sealed class ModFileWithHash : ModFile
{
    /// <summary>The MD5 file hash.</summary>
    [JsonProperty("md5")]
    public string Md5 { get; set; } = null!;
}