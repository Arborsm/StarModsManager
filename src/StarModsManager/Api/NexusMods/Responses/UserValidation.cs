// ReSharper disable InconsistentNaming
using System.Text.Json.Serialization;

namespace StarModsManager.Api.NexusMods.Responses;

/// <summary>User login metadata.</summary>
public sealed class UserValidation
{
    /// <summary>The unique user ID.</summary>
    [JsonPropertyName("user_id")]
    public int UserID { get; set; }

    /// <summary>The user's API authentication key.</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = null!;

    /// <summary>The username.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    /// <summary>The user's email address.</summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = null!;

    /// <summary>The URL of the user's avatar.</summary>
    [JsonPropertyName("profile_url")]
    public string ProfileUrl { get; set; } = null!;

    /// <summary>Whether the user has a premium Nexus account.</summary>
    [JsonPropertyName("is_premium")]
    public bool IsPremium { get; set; }

    /// <summary>Whether the user has a supporter Nexus account.</summary>
    [JsonPropertyName("is_supporter")]
    public bool IsSupporter { get; set; }
}