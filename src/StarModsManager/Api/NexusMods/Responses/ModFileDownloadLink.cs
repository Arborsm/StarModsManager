using System.Text.Json.Serialization;

namespace StarModsManager.Api.NexusMods.Responses;

/// <summary>A mod file download link.</summary>
public sealed class ModFileDownloadLink
{
    /// <summary>The full name of the CDN serving the file.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    /// <summary>The short name of the CDN serving the file.</summary>
    [JsonPropertyName("short_name")]
    public string ShortName { get; set; } = null!;

    /// <summary>The download URL.</summary>
    [JsonPropertyName("URI")]
    public string Uri { get; set; } = null!;
}

public sealed class DownloadResponse
{
    [JsonPropertyName("url")]
    public Uri? Url { get; set; }
}