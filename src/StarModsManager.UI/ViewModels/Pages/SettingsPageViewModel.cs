﻿using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.Styling;
using Serilog;
using StarModsManager.Api;
using StarModsManager.Assets;
using StarModsManager.Config;
using StarModsManager.Lib;

namespace StarModsManager.ViewModels.Pages;

public partial class SettingsPageViewModel : MainPageViewModelBase
{
    private const string System = "System";
    private const string Dark = "Dark";
    private const string Light = "Light";
    private readonly FluentAvaloniaTheme _faTheme;

    public SettingsPageViewModel()
    {
        _faTheme = (FluentAvaloniaTheme)Application.Current!.Styles.First(it => it is FluentAvaloniaTheme);
        GetPredefColors();
    }

    public MainConfig MainConfig => Services.MainConfig;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CustomAccentColor))]
    public partial string CurrentAppTheme { get; set; } = null!;

    [ObservableProperty]
    public partial FlowDirection CurrentFlowDirection { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ListBoxColor))]
    public partial Color CustomAccentColor { get; set; }

    [ObservableProperty]
    public partial Color? ListBoxColor { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CustomAccentColor))]
    [NotifyPropertyChangedFor(nameof(ListBoxColor))]
    public partial bool UseCustomAccentColor { get; set; }

    public override string NavHeader => NavigationService.Settings;

    public string[] AppThemes { get; } = [System, Light, Dark /*, FluentAvaloniaTheme.HighContrastTheme*/];

    public FlowDirection[] AppFlowDirections { get; } = [FlowDirection.LeftToRight, FlowDirection.RightToLeft];

    public List<Color> PredefinedColors { get; private set; } = [];
    public string VersionText => $"{Lang.ModAuthor}: Arborsm, {Lang.CurrentVersion}: {Services.AppVersion}";

    public void Init()
    {
        CurrentAppTheme = MainConfig.AppTheme;
        CurrentFlowDirection = AppFlowDirections[MainConfig.AppFlowDirections];
        CustomAccentColor = Color.FromUInt32(MainConfig.AppAccentColor);
        UseCustomAccentColor = MainConfig.UseCustomAccentColor;
    }

    private static ThemeVariant GetThemeVariant(string value)
    {
        return value switch { Light => ThemeVariant.Light, _ => ThemeVariant.Dark };
    }

    private void UpdateAppAccentColor(Color? color)
    {
        _faTheme.CustomAccentColor = color;
        if (color is null) return;
        MainConfig.AppAccentColor = color.Value.ToUInt32();
    }

    partial void OnCurrentAppThemeChanged(string value)
    {
        var newTheme = GetThemeVariant(value);
        MainConfig.AppTheme = value;
        Application.Current!.RequestedThemeVariant = newTheme;
        _faTheme.PreferSystemTheme = value == System;
    }

    partial void OnCurrentFlowDirectionChanged(FlowDirection value)
    {
        var lifetime = Application.Current!.ApplicationLifetime;
        MainConfig.AppFlowDirections = (int)value;
        switch (lifetime)
        {
            case IClassicDesktopStyleApplicationLifetime cdl when cdl.MainWindow!.FlowDirection == value:
                return;
            case IClassicDesktopStyleApplicationLifetime cdl:
                cdl.MainWindow!.FlowDirection = value;
                break;
            case ISingleViewApplicationLifetime single:
            {
                var mainWindow = TopLevel.GetTopLevel(single.MainView);
                if (mainWindow!.FlowDirection == value)
                    return;
                mainWindow.FlowDirection = value;
                break;
            }
        }
    }

    partial void OnUseCustomAccentColorChanged(bool value)
    {
        MainConfig.UseCustomAccentColor = value;
        if (value)
        {
            if (_faTheme.TryGetResource("SystemAccentColor", null, out var curColor))
            {
                CustomAccentColor = (Color)curColor;
                ListBoxColor = CustomAccentColor;
            }
            else
            {
                // This should never happen, if it does, something bad has happened
                throw new Exception("Unable to retrieve SystemAccentColor");
            }
        }
        else
        {
            CustomAccentColor = default;
            ListBoxColor = default;
            UpdateAppAccentColor(null);
        }
    }

    partial void OnCustomAccentColorChanged(Color value)
    {
        ListBoxColor = value;
        UpdateAppAccentColor(value);
    }

    partial void OnListBoxColorChanged(Color? value)
    {
        if (value == null) return;
        CustomAccentColor = value.Value;
        UpdateAppAccentColor(value.Value);
    }

    [RelayCommand]
    private void CheckUpdate()
    {
        UpdateChecker.CheckUpdate();
    }

    [RelayCommand]
    private void OpenModDirectory()
    {
        PlatformHelper.OpenFileOrUrl(MainConfig.DirectoryPath);
        Log.Information("Opened mods directory: {path}", MainConfig.DirectoryPath);
    }

    [RelayCommand]
    private void AutoSelect()
    {
        var path = Path.Combine(ModsHelper.Instance.GameFolders.First(), "mods");
        if (!Directory.Exists(path))
        {
            Services.Notification?.Show(Lang.ModsDirNotFound);
            return;
        }

        MainConfig.DirectoryPath = path;
        Log.Information("Auto selected mods directory: {path}", MainConfig.DirectoryPath);
    }

    [RelayCommand]
    private void GetApiKey()
    {
        const string url = "https://www.nexusmods.com/users/myaccount?tab=api";
        PlatformHelper.OpenFileOrUrl(url);
    }

    [RelayCommand]
    private void GetCookie()
    {
        Services.Notification?.Show(Lang.GetCookiesText);
    }

    private void GetPredefColors()
    {
        PredefinedColors =
        [
            Color.FromRgb(255, 185, 0),
            Color.FromRgb(255, 140, 0),
            Color.FromRgb(247, 99, 12),
            Color.FromRgb(202, 80, 16),
            Color.FromRgb(218, 59, 1),
            Color.FromRgb(239, 105, 80),
            Color.FromRgb(209, 52, 56),
            Color.FromRgb(255, 67, 67),
            Color.FromRgb(231, 72, 86),
            Color.FromRgb(232, 17, 35),
            Color.FromRgb(234, 0, 94),
            Color.FromRgb(195, 0, 82),
            Color.FromRgb(227, 0, 140),
            Color.FromRgb(191, 0, 119),
            Color.FromRgb(194, 57, 179),
            Color.FromRgb(154, 0, 137),
            Color.FromRgb(0, 120, 212),
            Color.FromRgb(0, 99, 177),
            Color.FromRgb(142, 140, 216),
            Color.FromRgb(107, 105, 214),
            Color.FromRgb(135, 100, 184),
            Color.FromRgb(116, 77, 169),
            Color.FromRgb(177, 70, 194),
            Color.FromRgb(136, 23, 152),
            Color.FromRgb(0, 153, 188),
            Color.FromRgb(45, 125, 154),
            Color.FromRgb(0, 183, 195),
            Color.FromRgb(3, 131, 135),
            Color.FromRgb(0, 178, 148),
            Color.FromRgb(1, 133, 116),
            Color.FromRgb(0, 204, 106),
            Color.FromRgb(16, 137, 62),
            Color.FromRgb(122, 117, 116),
            Color.FromRgb(93, 90, 88),
            Color.FromRgb(104, 118, 138),
            Color.FromRgb(81, 92, 107),
            Color.FromRgb(86, 124, 115),
            Color.FromRgb(72, 104, 96),
            Color.FromRgb(73, 130, 5),
            Color.FromRgb(16, 124, 16),
            Color.FromRgb(118, 118, 118),
            Color.FromRgb(76, 74, 72),
            Color.FromRgb(105, 121, 126),
            Color.FromRgb(74, 84, 89),
            Color.FromRgb(100, 124, 100),
            Color.FromRgb(82, 94, 84),
            Color.FromRgb(132, 117, 69),
            Color.FromRgb(126, 115, 95)
        ];
    }
}