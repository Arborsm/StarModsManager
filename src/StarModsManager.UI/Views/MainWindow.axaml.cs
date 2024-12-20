using System.Drawing;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using StarModsManager.Api;
using StarModsManager.Assets;
using StarModsManager.ViewModels.Customs;
using StarModsManager.Views.Customs;

namespace StarModsManager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        if (Services.MainConfig.ClientSize != Size.Empty)
        {
            WindowState = WindowState.Normal;
            Width = Services.MainConfig.ClientSize.Width;
            Height = Services.MainConfig.ClientSize.Height;
        }

        InitializeComponent();
        Services.Dialog = new Dialog(GetTopLevel(this)!);
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragEnterEvent, DragInput);
        AddHandler(DragDrop.DragOverEvent, DragInput);
        AddHandler(DragDrop.DropEvent, Drop);
        HitBox.PointerPressed += HitBoxOnPointerPressed;
        HitBox.DoubleTapped += HitBoxOnDoubleTapped;
    }

    private static void HitBoxOnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control control) return;
        var topLevel = GetTopLevel(control);
        if (topLevel is Window window)
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
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
        if (installModViewModel.Files.Count == 0) return;
        var dialog = new ContentDialog
        {
            Title = Lang.InstallingMods,
            Content = installModView,
            PrimaryButtonText = Lang.OK,
            PrimaryButtonCommand = installModViewModel.InstallCommand,
            CloseButtonText = Lang.Cancel
        };
        dialog.ShowAsync();
        e.Handled = true;
    }
}

public class Dialog(TopLevel topLevel) : IDialog
{
    public async Task<IReadOnlyList<IStorageFile>> ShowPickupFilesDialogAsync(string title, bool allowMultiple,
        IReadOnlyList<FilePickerFileType>? fileTypeFilter)
    {
        return await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = allowMultiple,
            FileTypeFilter = fileTypeFilter
        });
    }

    public async Task<string?> ShowDownloadDialogAsync()
    {
        var dialog = new ContentDialog
        {
            Title = Lang.NewDownload,
            Content = new TextBox
            {
                Watermark = Lang.EnterURL,
                Width = 400
            },
            PrimaryButtonText = Lang.OK,
            CloseButtonText = Lang.Cancel
        };
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? (dialog.Content as TextBox)?.Text : null;
    }
}