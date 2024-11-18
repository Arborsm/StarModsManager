using System.Drawing;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Serilog;
using StarModsManager.Api;

namespace StarModsManager.Views.Pages;

public partial class SettingsPageView : UserControl
{
    public SettingsPageView()
    {
        InitializeComponent();
    }

    private async void OpenModFolderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var folder = await topLevel!.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select LocalModsMap Folder",
                AllowMultiple = false
            });
            if (folder.Count > 0) Services.MainConfig.DirectoryPath = folder[0].Path.LocalPath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error Opening Folder");
        }
    }

    private void SaveWindowSizeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        Services.MainConfig.ClientSize = new Size((int)topLevel!.Width, (int)topLevel.Height);
        Log.Information("Saved Window Size: {Size}", topLevel.ClientSize);
    }

    private void ClearSizeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Services.MainConfig.ClientSize = Size.Empty;
        Log.Information("Cleared Window Size");
    }
}