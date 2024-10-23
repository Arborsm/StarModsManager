using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace StarModsManager.Api.NexusMods.Responses;

/// <summary>A list of files for a mod.</summary>
public sealed class ModFileList
{
    /// <summary>The matched file details.</summary>
    [JsonProperty("files")]
    public ModFile[] Files { get; set; } = null!;

    /// <summary>The update relationships between files (i.e. a record of the uploader marking each file as a newer version of a previous one, if they did).</summary>
    [JsonProperty("file_updates")]
    public ModFileUpdate[] FileUpdates { get; set; } = null!;
}

/// <summary>A record indicating an update relationship between two files (e.g. 1.0.1 supersedes 1.0.0).</summary>
public sealed class ModFileUpdate
{
    /// <summary>The older file ID.</summary>
    [JsonProperty("old_file_id")]
    public int OldFileId { get; set; }

    /// <summary>The older filename.</summary>
    [JsonProperty("old_file_name")]
    public string OldFileName { get; set; } = null!;

    /// <summary>The newer file ID.</summary>
    [JsonProperty("new_file_id")]
    public int NewFileId { get; set; }

    /// <summary>The newer filename.</summary>
    [JsonProperty("new_file_name")]
    public string NewFileName { get; set; } = null!;

    /// <summary>When the newer file was uploaded.</summary>
    [JsonProperty("uploaded_timestamp")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTimeOffset UploadTimestamp { get; set; }
}