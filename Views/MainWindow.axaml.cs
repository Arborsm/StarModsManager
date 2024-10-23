using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using StarModsManager.Api.lib;

namespace StarModsManager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.Register<DialogMessage>(this, ShowDialogAsync);
        WeakReferenceMessenger.Default.Register<NotificationMessage>(this, ShowNotification);
    }

    private void ShowNotification(object recipient, NotificationMessage message)
    {
        var infoBar = this.FindControl<InfoBar>("MainInfoBar")!;
        if (message.Severity == InfoBarSeverity.Informational)
        {
            infoBar.Background = 
                Application.Current!.RequestedThemeVariant == ThemeVariant.Light ? Brushes.Azure : Brushes.MediumSlateBlue;
        }
        infoBar.IsClosable = message.IsClosable;
        infoBar.Title = message.Title;
        infoBar.Message = message.Message;
        infoBar.Severity = message.Severity;
        infoBar.IsIconVisible = message.IsIconVisible;
        infoBar.IsOpen = true;
    }

    private async void ShowDialogAsync(object recipient, DialogMessage message)
    {
        var files = await GetTopLevel(this)!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = message.Title,
            AllowMultiple = message.AllowMultiple,
            FileTypeFilter = message.FileTypeFilter
        });
        message.CompletionSource.SetResult(files);
    }
}