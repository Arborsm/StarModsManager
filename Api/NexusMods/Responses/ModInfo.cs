// ReSharper disable InconsistentNaming

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace StarModsManager.Api.NexusMods.Responses;

/// <summary>A Nexus mod model.</summary>
public sealed class ModInfo
{
    /// <summary>The mod name.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = null!;

    /// <summary>The short mod summary.</summary>
    [JsonProperty("summary")]
    public string Summary { get; set; } = null!;

    /// <summary>The long mod description in BBCode format.</summary>
    [JsonProperty("description")]
    public string Description { get; set; } = null!;

    /// <summary>The URL for the main image shown in thumbnails.</summary>
    [JsonProperty("picture_url")]
    public Uri PictureUrl { get; set; } = null!;

    /// <summary>The unique mod ID.</summary>
    [JsonProperty("mod_id")]
    public int ModID { get; set; }

    /// <summary>The unique game ID.</summary>
    [JsonProperty("game_id")]
    public int GameID { get; set; }

    /// <summary>The game key.</summary>
    [JsonProperty("domain_name")]
    public string DomainName { get; set; } = null!;

    /// <summary>The mod category ID.</summary>
    [JsonProperty("category_id")]
    public int CategoryID { get; set; }

    /// <summary>The mod version number.</summary>
    [JsonProperty("version")]
    public string Version { get; set; } = null!;

    /// <summary>When the mod was created.</summary>
    [JsonProperty("created_timestamp")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTimeOffset Created { get; set; }

    /// <summary>When the mod was created.</summary>
    [JsonProperty("updated_timestamp")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTimeOffset Updated { get; set; }

    /// <summary>The user-defined 'created by' author name.</summary>
    [JsonProperty("author")]
    public string Author { get; set; } = null!;

    /// <summary>The mod author's username.</summary>
    [JsonProperty("uploaded_by")]
    public string UploadedBy { get; set; } = null!;

    /// <summary>The mod author's profile URL.</summary>
    [JsonProperty("uploaded_users_profile_url")]
    public Uri UploadedByProfileUrl { get; set; } = null!;

    /// <summary>Whether the mod contains adult content such as gore, nudity, or extreme violence.</summary>
    [JsonProperty("contains_adult_content")]
    public bool ContainsAdultContent { get; set; }

    /// <summary>The mod publication status.</summary>
    [JsonProperty("status")]
    public ModStatus Status { get; set; }

    /// <summary>Whether the mod is published and available.</summary>
    [JsonProperty("available")]
    public bool IsAvailable { get; set; }

    /// <summary>The user who uploaded them od.</summary>
    [JsonProperty("user")]
    public UserValidation UserValidation { get; set; } = null!;

    /// <summary>The user's endorsement status with this mod, or <c>null</c> if not applicable.</summary>
    [JsonProperty("endorsement")]
    public Endorsement Endorsement { get; set; } = null!;
}

/// <summary>A mod publication status.</summary>
public enum ModStatus
{
    /// <summary>Not applicable.</summary>
    [EnumMember(Value = "none")]
    None,

    /// <summary>The mod is not yet published.</summary>
    [EnumMember(Value = "not_published")]
    NotPublished,

    /// <summary>The mod is published and visible.</summary>
    [EnumMember(Value = "published")]
    Published,

    /// <summary>The mod should be published once the game goes live.</summary>
    [EnumMember(Value = "publish_with_game")]
    PublishWithGame,

    /// <summary>The mod has been hidden by the author.</summary>
    [EnumMember(Value = "hidden")]
    Hidden,

    /// <summary>The mod is hidden while it undergoes moderator review.</summary>
    [EnumMember(Value = "under_moderation")]
    UnderModeration,

    /// <summary>The mod has been recoverably deleted.</summary>
    [EnumMember(Value = "waste_binned")]
    Wastebinned,

    /// <summary>The mod has been permanently removed.</summary>
    [EnumMember(Value = "removed")]
    Removed
}

/// <summary>Simplified data about a mod endorsement.</summary>
public sealed class Endorsement
{
    /// <summary>The mod endorsement status.</summary>
    [JsonProperty("endorse_status")]
    public EndorsementStatus EndorseStatus { get; set; }

    /// <summary>When the user endorsed the mod (if they did).</summary>
    [JsonProperty("timestamp")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>The current mod version when the user endorsed the mod.</summary>
    [JsonProperty("version")]
    public string Version { get; set; } = null!;
}

/// <summary>A status representing whether the user has endorsed a mod.</summary>
public enum EndorsementStatus
{
    /// <summary>Not applicable.</summary>
    None,

    /// <summary>The user is eligible to endorse the mod, but hasn't done so yet.</summary>
    Undecided,

    /// <summary>The user endorsed and subsequently un-endorsed the mod.</summary>
    Abstained,

    /// <summary>The user has endorsed the mod.</summary>
    Endorsed
}