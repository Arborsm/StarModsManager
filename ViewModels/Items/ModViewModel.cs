using System.Diagnostics;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using StarModsManager.Api;
using StarModsManager.Common.Main;
using StarModsManager.Common.Mods;
using StarModsManager.ViewModels.Customs;
using PicsSelectView = StarModsManager.Views.Customs.PicsSelectView;

namespace StarModsManager.ViewModels.Items;

public partial class ModViewModel : ViewModelBase
{
    [ObservableProperty]
    private Bitmap? _pic;
    [ObservableProperty]
    private bool _isDisabled;

    public ModViewModel(OnlineMod onlineMod, LocalMod? localMod = default)
    {
        OnlineMod = onlineMod;
        LocalMod = localMod;
        if (string.IsNullOrEmpty(onlineMod.ModId) && localMod == null)
        {
            Task.Run(OnlineMod.SaveAsync);
        }
        else if (localMod is not null)
        {
            IsDisabled = Path.GetFileName(localMod.PathS).StartsWith('.');
            _localModPath = localMod.PathS;
        }
    }

    // Test
    public ModViewModel() : this(
        new OnlineMod())
    {
        Pic = new Bitmap("D:\\Users\\26537\\AppData\\Roaming\\StarModsManager\\Cache\\14070\\14070-1665488527-1800567611.bmp");
        IsDisabled = true;
    }

    public OnlineMod OnlineMod { get; }
    public LocalMod? LocalMod { get; }
    public bool LocalModIsNotNull => LocalMod is not null;

    private bool IsLocal => LocalMod is not null;
    private string? _localModPath;

    [RelayCommand(CanExecute = nameof(IsLocal))]
    private async Task ChangeCover()
    {
        if (LocalMod is not null)
        {
            var content = new PicsSelectViewModel(this);
            var dialog = new ContentDialog
            {
                Title = "Change Cover",
                PrimaryButtonText = "Select",
                SecondaryButtonText = "Custom",
                CloseButtonText = "Close",
                Content = new PicsSelectView
                {
                    Content = content
                },
                PrimaryButtonCommand = content.SelectPicCommand,
                SecondaryButtonCommand = content.CustomPicCommand
            };

            await dialog.ShowAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(IsLocal))]
    private void OpenModFolder()
    {
        if (LocalMod is not null) Process.Start("explorer.exe", LocalMod.PathS);
    }

    [RelayCommand]
    private void OpenUrl()
    {
        if (OnlineMod.Url is not null) Process.Start("explorer.exe", OnlineMod.Url);
    }

    [RelayCommand]
    private async Task AsyncLoadCover()
    {
        await LoadCover(TimeSpan.Zero, CancellationToken.None, true);
    }
    
    [RelayCommand(CanExecute = nameof(IsLocal))]
    private void SwitchMod() => IsDisabled = ToggleDotPrefix();
    
    private bool ToggleDotPrefix()
    {
        if (!Directory.Exists(_localModPath)) return IsDisabled;

        var directoryName = Path.GetFileName(_localModPath);
        var parentDirectory = Path.GetDirectoryName(_localModPath)!;
        var newDirectoryName = directoryName.StartsWith('.') ? directoryName[1..] : '.' + directoryName;
        var newPath = Path.Combine(parentDirectory, newDirectoryName);

        Directory.Move(_localModPath, newPath);
        _localModPath = newPath;
        return !IsDisabled;
    }

    public async Task LoadCover(TimeSpan delay, CancellationToken cancellationToken, bool refresh = false)
    {
        if (LocalMod is not null && File.Exists(LocalMod.InfoPicturePath))
        {
            Pic = await Task.Run(() => new Bitmap(LocalMod.InfoPicturePath), cancellationToken);
            return;
        }

        if (refresh) StarDebug.Debug(@"Refreshing Cover");
        try
        {
            await using var imageStream = await OnlineMod.LoadPicBitmapAsync(delay, refresh, cancellationToken);
            if (imageStream is not null)
                Pic = await Task.Run(() => Bitmap.DecodeToWidth(imageStream, 400), cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}