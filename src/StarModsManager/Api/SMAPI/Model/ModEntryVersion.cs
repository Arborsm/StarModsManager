namespace StarModsManager.Api.SMAPI.Model;

public class ModEntryVersion
{
    /*********
     ** Public methods
     *********/
    /// <summary>Construct an instance.</summary>
    public ModEntryVersion()
    {
    }

    /// <summary>Construct an instance.</summary>
    /// <param name="version">The version number.</param>
    /// <param name="url">The mod page URL.</param>
    public ModEntryVersion(string version, string url)
    {
        this.Version = version;
        this.Url = url;
    }
    // Based on SMAPI's ModEntryVersionModel.cs: https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI.Toolkit/Framework/Clients/WebApi/ModEntryVersionModel.cs

    /*********
     ** Accessors
     *********/
    /// <summary>The version number.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>The mod page URL.</summary>
    public string Url { get; set; } = string.Empty;
}