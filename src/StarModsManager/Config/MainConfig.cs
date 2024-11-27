using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization.Metadata;
using StarModsManager.Api;
using StarModsManager.Api.NexusMods;
using StarModsManager.Api.NexusMods.Interface;

namespace StarModsManager.Config;

public class MainConfig : ConfigBase
{
    public MainConfig()
    {
        if (!Directory.Exists(CachePath)) Directory.CreateDirectory(CachePath);
    }

    public string CachePath
    {
        get;
        set => SetProperty(ref field, value);
    } = Path.Combine(Services.AppSavingPath, "Cache");

    public bool Debug
    {
        get;
        set
        {
            SetProperty(ref field, value);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            if (value)
                DebugHelper.ShowConsole();
            else
                DebugHelper.HideConsole();
        }
    }

    public bool AutoCheckUpdates
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    public string DirectoryPath
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && IsLoaded)
                Services.LifeCycle.Reset();
        }
    } = ModsHelper.Instance.GameFolders.FirstOrDefault(Services.AppSavingPath);

    public string NexusModsApiKey
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && IsLoaded)
            {
                NexusManager.Initialize(value, $"StarModsManager/{Services.AppVersion}");
                NexusDownload.Instance.Reset();
            }
        }
    } = string.Empty;

    public string NexusModsCookie
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                NexusWebClient.Instance.SetCookie(value);
                NexusDownload.Instance.Reset();
            }
        }
    } = string.Empty;

    public Size ClientSize
    {
        get;
        set => SetProperty(ref field, value);
    } = Size.Empty;

    public string AppTheme
    {
        get;
        set => SetProperty(ref field, value);
    } = "System";

    public int AppFlowDirections
    {
        get;
        set => SetProperty(ref field, value);
    }

    public uint AppAccentColor
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool UseCustomAccentColor
    {
        get;
        set => SetProperty(ref field, value);
    }

    protected override JsonTypeInfo GetJsonTypeInfo()
    {
        return ConfigContent.Default.MainConfig;
    }

    public static MainConfig LoadOrCreate()
    {
        var config = Load<MainConfig>(ConfigContent.Default.MainConfig);
        config ??= new MainConfig();
        config.IsLoaded = true;
        return config;
    }
}