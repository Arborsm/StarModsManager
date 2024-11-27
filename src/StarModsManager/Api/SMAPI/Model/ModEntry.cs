namespace StarModsManager.Api.SMAPI.Model;

public class ModEntry
{
    // Based on SMAPI's ModEntryModel.cs: https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI.Toolkit/Framework/Clients/WebApi/ModEntryModel.cs

    /// <summary>The mod's unique ID.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The update version recommended by the web API based on its version update and mapping rules.</summary>
    public ModEntryVersion SuggestedUpdate { get; set; } = new();

    /// <summary>Optional extended data which isn't needed for update checks.</summary>
    public ModEntryMetadata Metadata { get; set; } = new();

    /// <summary>The errors that occurred while fetching update data.</summary>
    public string[] Errors { get; set; } = [];
}