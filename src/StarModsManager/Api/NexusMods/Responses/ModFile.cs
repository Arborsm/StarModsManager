using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using StarModsManager.Api.NexusMods.Interface;

namespace StarModsManager.Api.NexusMods.Responses;

/// <summary>A downloadable mod file.</summary>
public class ModFile
{
    /// <summary>The unique file ID.</summary>
    [JsonPropertyName("file_id")]
    public int FileId { get; set; }

    /// <summary>The download name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    /// <summary>The file version number.</summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = null!;

    /// <summary>The mod version at the time the file was uploaded.</summary>
    [JsonPropertyName("mod_version")]
    public string ModVersion { get; set; } = null!;

    /// <summary>The download filename.</summary>
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = null!;

    /// <summary>The file category.</summary>
    [JsonPropertyName("category_id")]
    public FileCategory Category { get; set; }

    /// <summary>Whether the file is marked as the primary download.</summary>
    [JsonPropertyName("is_primary")]
    public bool IsPrimary { get; set; }

    /// <summary>The file size in kilobytes.</summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    /// <summary>When the file was uploaded.</summary>
    [JsonPropertyName("uploaded_timestamp")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTimeOffset UploadedTimestamp { get; set; }

    /// <summary>The URL to the external virus scan results.</summary>
    [JsonPropertyName("external_virus_scan_url")]
    public Uri ExternalVirusScanUri { get; set; } = null!;

    /// <summary>The HTML change logs, if any.</summary>
    [JsonPropertyName("changelog_html")]
    public string ChangeLogHtml { get; set; } = null!;
}

/// <summary>A mod file category.</summary>
public enum FileCategory
{
    /// <summary>A main file.</summary>
    [EnumMember(Value = "MAIN")]
    Main = 1,

    /// <summary>An update file.</summary>
    [EnumMember(Value = "UPDATE")]
    Update = 2,

    /// <summary>An optional file.</summary>
    [EnumMember(Value = "OPTIONAL")]
    Optional = 3,

    [EnumMember(Value = "OLD_VERSION")]
    Old = 4,

    /// <summary>A miscellaneous file.</summary>
    [EnumMember(Value = "MISCELLANEOUS")]
    Miscellaneous = 5,

    /// <summary>A deleted file not shown in the UI.</summary>
    [EnumMember(Value = "DELETED")]
    Deleted = 6
}