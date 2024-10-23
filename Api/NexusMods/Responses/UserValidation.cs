// ReSharper disable InconsistentNaming

using Newtonsoft.Json;

namespace StarModsManager.Api.NexusMods.Responses;

/// <summary>User login metadata.</summary>
public sealed class UserValidation
{
    /// <summary>The unique user ID.</summary>
    [JsonProperty("user_id")]
    public int UserID { get; set; }

    /// <summary>The user's API authentication key.</summary>
    [JsonProperty("key")]
    public string Key { get; set; } = null!;

    /// <summary>The username.</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = null!;

    /// <summary>The user's email address.</summary>
    [JsonProperty("email")]
    public string Email { get; set; } = null!;

    /// <summary>The URL of the user's avatar.</summary>
    [JsonProperty("profile_url")]
    public string ProfileUrl { get; set; } = null!;

    /// <summary>Whether the user has a premium Nexus account.</summary>
    [JsonProperty("is_premium")]
    public bool IsPremium { get; set; }

    /// <summary>Whether the user has a supporter Nexus account.</summary>
    [JsonProperty("is_supporter")]
    public bool IsSupporter { get; set; }

}