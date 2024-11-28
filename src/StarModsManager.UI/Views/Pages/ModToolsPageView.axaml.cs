using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Serilog;
using StarModsManager.Api;
using StarModsManager.Lib;

namespace StarModsManager.Views.Pages;

public partial class ModToolsPageView : UserControl
{
    public ModToolsPageView()
    {
        InitializeComponent();
    }

    private async void ApiSettingsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = "API Settings",
                Content = new TextBox
                {
                    Text = Services.MainConfig.NexusModsApiKey
                },
                PrimaryButtonText = "Save",
                CloseButtonText = "Close"
            };
            dialog.PrimaryButtonCommand = new RelayCommand(() =>
            {
                Services.MainConfig.NexusModsApiKey = (dialog.Content as TextBox)?.Text ?? "";
            });
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in ModToolsPageView");
        }
    }

    private async void CookiesSettingsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = "Cookies Settings",
                Content = new TextBox
                {
                    Text = Services.MainConfig.NexusModsCookie
                },
                PrimaryButtonText = "Save",
                CloseButtonText = "Close"
            };
            dialog.PrimaryButtonCommand = new RelayCommand(() =>
            {
                Services.MainConfig.NexusModsCookie = (dialog.Content as TextBox)?.Text ?? "";
            });
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in ModToolsPageView");
        }
    }

    private async void TransSettingsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await NavigationService.ShowTranslationSetting();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in ModToolsPageView");
        }
    }
}