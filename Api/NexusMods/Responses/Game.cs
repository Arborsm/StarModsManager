﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace StarModsManager.Api.NexusMods.Responses;

/// <summary>A Nexus game model.</summary>
    public sealed class Game
    {
        /// <summary>The unique game ID.</summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>The game name.</summary>
        [JsonProperty("name")]
        public string Name { get; set; } = null!;
        
        /// <summary>The URL to the Nexus Mods forum for this game.</summary>
        [JsonProperty("forum_url")]
        public Uri ForumUrl { get; set; } = null!;

        /// <summary>The URL to the Nexus Mods front page for this game.</summary>
        [JsonProperty("nexusmods_url")]
        public Uri NexusModsUrl { get; set; } = null!;

        /// <summary>A brief description of the game genre, like 'Adventure' or 'Dungeon crawl'.</summary>
        [JsonProperty("genre")]
        public string Genre { get; set; } = null!;

        /// <summary>The number of files uploaded for the game's mods.</summary>
        [JsonProperty("file_count")]
        public long FileCount { get; set; }

        /// <summary>The number of file downloads for the game's mods.</summary>
        [JsonProperty("downloads")]
        public long Downloads { get; set; }

        /// <summary>The unique game key.</summary>
        [JsonProperty("domain_name")]
        public string DomainName { get; set; } = null!;

        /// <summary>When the game became approved and available on Nexus Mods, if available. This may be null for very old approved games.</summary>
        [JsonProperty("approved_date")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTimeOffset? ApprovedDate { get; set; }

        /// <summary>The number of page views for the game's mods.</summary>
        [JsonProperty("file_views")]
        public long FileViews { get; set; }

        /// <summary>The number of users who created a mod page for the game.</summary>
        [JsonProperty("authors")]
        public long Authors { get; set; }

        /// <summary>The number of mod endorsements for the game's mods.</summary>
        [JsonProperty("file_endorsements")]
        public long FileEndorsements { get; set; }

        /// <summary>The number of mods created for the game.</summary>
        [JsonProperty("mods")]
        public long Mods { get; set; }

        /// <summary>The mod categories defined for this game.</summary>
        [JsonProperty("categories")]
        public GameCategory[] Categories { get; set; } = null!;
    }
    
    /// <summary>A mod category available for a game's mods.</summary>
    public sealed class GameCategory
    {
        /// <summary>The category ID.</summary>
        [JsonProperty("category_id")]
        public int Id { get; set; }

        /// <summary>The category name.</summary>
        [JsonProperty("name")]
        public string Name { get; set; } = null!;

        /// <summary>The parent category, if any.</summary>
        [JsonProperty("parent_category")]
        public int? ParentCategory { get; set; }
    }