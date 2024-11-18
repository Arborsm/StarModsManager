﻿using System.Drawing;
using System.Text.Json.Serialization.Metadata;
using StarModsManager.Api;
using StarModsManager.Api.NexusMods;
using StarModsManager.Api.NexusMods.Interface;

namespace StarModsManager.Config;

public class MainConfig : ConfigBase
{
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
            if (value)
            {
                DebugHelper.ShowConsole();
            }
            else
            {
                DebugHelper.HideConsole();
            }
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
                Services.Notification.Show("注意", "需要重启使得改动生效", Severity.Warning);
        }
    } = ModsHelper.Instance.GameFolders.Select(x => x.FullName).FirstOrDefault(Services.AppSavingPath);

    public string NexusModsApiKey
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && IsLoaded)
                NexusManager.Initialize(value, $"StarModsManager/{Services.AppVersion}");
        }
    } = string.Empty;

    public string NexusModsCookie
    {
        get;
        set
        {
            if (SetProperty(ref field, value)) NexusWebClient.Instance.SetCookie(value);
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

    public MainConfig()
    {
        if (!Directory.Exists(CachePath)) Directory.CreateDirectory(CachePath);
    }

    protected override JsonTypeInfo GetJsonTypeInfo() => ConfigContent.Default.MainConfig;

    public static MainConfig LoadOrCreate()
    {
        var config = Load<MainConfig>(ConfigContent.Default.MainConfig);
        config ??= new MainConfig();
        config.IsLoaded = true;
        return config;
    }
}