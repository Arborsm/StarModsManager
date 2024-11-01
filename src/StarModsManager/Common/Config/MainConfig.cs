using System.Text.Json.Serialization.Metadata;
using CommunityToolkit.Mvvm.ComponentModel;
using StarModsManager.Api;
using StarModsManager.Api.NexusMods;

namespace StarModsManager.Common.Config;

public partial class MainConfig : ConfigBase
{
    [ObservableProperty]
    private string _cachePath = Path.Combine(Services.AppSavingPath, "Cache");

    [ObservableProperty]
    private bool _debug;

    [ObservableProperty]
    private string _directoryPath = AppDomain.CurrentDomain.BaseDirectory;

    [ObservableProperty]
    private string _nexusModsApiKey = string.Empty;

    public MainConfig()
    {
        if (!Directory.Exists(CachePath)) Directory.CreateDirectory(CachePath);
    }

    partial void OnNexusModsApiKeyChanged(string? oldValue, string newValue)
    {
        if (string.IsNullOrWhiteSpace(newValue) || newValue == oldValue) return;
        NexusManager.Initialize(newValue, $"StarModsManager/{Services.AppVersion}");
    }

    protected override JsonTypeInfo GetJsonTypeInfo()
    {
        return ConfigContent.Default.MainConfigContent;
    }

    protected override IConfigContent GetContent()
    {
        return new MainConfigContent
        {
            CachePath = this.CachePath,
            Debug = this.Debug,
            DirectoryPath = this.DirectoryPath,
            NexusModsApiKey = this.NexusModsApiKey
        };
    }

    protected override void LoadFromJson(object loadedConfig)
    {
        if (loadedConfig is not MainConfigContent config) return;
        this.CachePath = config.CachePath;
        this.Debug = config.Debug;
        this.DirectoryPath = config.DirectoryPath;
        this.NexusModsApiKey = config.NexusModsApiKey;
    }
}

public class MainConfigContent : IConfigContent
{
    public string CachePath { get; set; } = string.Empty;
    public bool Debug { get; set; }
    public string DirectoryPath { get; set; } = string.Empty;
    public string NexusModsApiKey { get; set; } = string.Empty;
}