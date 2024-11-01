using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using StarModsManager.ViewModels.Pages;

namespace StarModsManager.Views.Pages;

public partial class SettingsPageView : UserControl
{
    public SettingsPageView()
    {
        InitializeComponent();
    }

    private async void OpenModFolderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var folder = await topLevel!.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select LocalModsMap Folder",
            AllowMultiple = false
        });
        if (folder.Count > 0) (DataContext as SettingsPageViewModel)!.ModDir = folder[0].Path.LocalPath;
    }
}