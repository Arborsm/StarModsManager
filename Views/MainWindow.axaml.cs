using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using StarModsManager.Api.lib;
using StarModsManager.ViewModels.Customs;
using StarModsManager.Views.Customs;

namespace StarModsManager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.Register<PickupFilesDialogMessage>(this, ShowDialogAsync);
        WeakReferenceMessenger.Default.Register<NotificationMessage>(this, ShowNotification);
        DragDrop.SetAllowDrop(this, true);
        this.AddHandler(DragDrop.DragEnterEvent, DragInput);
        this.AddHandler(DragDrop.DragOverEvent, DragInput);
        this.AddHandler(DragDrop.DropEvent, Drop);
        HitBox.PointerPressed += HitBoxOnPointerPressed;
        HitBox.DoubleTapped += HitBoxOnDoubleTapped;
    }

    private static void HitBoxOnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control control) return;
        var topLevel = GetTopLevel(control);
        if (topLevel is Window window)
        {
            window.WindowState = window.WindowState == WindowState.Maximized 
                ? WindowState.Normal : WindowState.Maximized;
        }
    }

    private static void HitBoxOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control || !e.GetCurrentPoint(control).Properties.IsLeftButtonPressed) return;
        var topLevel = GetTopLevel(control);
        if (topLevel is Window window) window.BeginMoveDrag(e);
    }
    
    private static void DragInput(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files)) e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }
    
    private static void Drop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files)) return;
        var files = e.Data.GetFiles();
        if (files == null) return;
        var installModViewModel = new InstallModViewModel(files);
        var installModView = new InstallModView
        {
            DataContext = installModViewModel
        };
        var dialog = new ContentDialog
        {
            Title = "Installing Mod",
            Content = installModView,
            PrimaryButtonText = "OK",
            PrimaryButtonCommand = installModViewModel.InstallCommand,
            CloseButtonText = "Cancel"
        };
        dialog.ShowAsync();
        e.Handled = true;
    }

    private void ShowNotification(object recipient, NotificationMessage message)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            var infoBar = this.FindControl<InfoBar>("MainInfoBar");
            if (infoBar == null) return;
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
        });
    }

    private async void ShowDialogAsync(object recipient, PickupFilesDialogMessage message)
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