using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using StarModsManager.Api;
using StarModsManager.Api.lib;
using StarModsManager.Api.NexusMods;
using StarModsManager.Common.Mods;
using StarModsManager.ViewModels.Customs;
using StarModsManager.Views.Customs;
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
        Pic = new Bitmap(
            "D:\\Users\\26537\\AppData\\Roaming\\StarModsManager\\Cache\\14070\\14070-1665488527-1800567611.bmp");
        IsDisabled = true;
    }

    public OnlineMod OnlineMod { get; }
    public LocalMod? LocalMod { get; }

    private bool IsLocal => LocalMod is not null;
    private string? _localModPath;

    [RelayCommand]
    private async Task DownloadAsync()
    {
        if (OnlineMod.Url != null) await new NexusDownload(OnlineMod.Url).GetModDownloadUrlAsync();
    }

    [RelayCommand]
    private async Task GetDetailAsync()
    {
        ViewModelBase content = LocalMod is not null 
            ? new ModDetailViewModel(await File.ReadAllTextAsync(Path.Combine(LocalMod.PathS, "manifest.json"))) 
            : new ModDetailViewModel(""); // ToDO
        var dialog = new ContentDialog
        {
            Title = "Detail",
            CloseButtonText = "Close",
            Content = new ModDetailView
            {
                Content = content
            }
        };

        await dialog.ShowAsync();
    }

    [RelayCommand(CanExecute = nameof(IsLocal))]
    private async Task ChangeCoverAsync()
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
                DataContext = content
            },
            PrimaryButtonCommand = content.SelectPicCommand,
            SecondaryButtonCommand = content.CustomPicCommand
        };

        await dialog.ShowAsync();
    }

    [RelayCommand(CanExecute = nameof(IsLocal))]
    private void HideMod()
    {
        LocalMod!.IsHidden = !LocalMod.IsHidden;
        WeakReferenceMessenger.Default.Send(new ModHiddenChangedMessage(LocalMod.UniqueID, LocalMod.IsHidden));
    }

    [RelayCommand(CanExecute = nameof(IsLocal))]
    private void OpenModFolder()
    {
        PlatformHelper.OpenFileOrUrl(LocalMod!.PathS);
    }

    [RelayCommand]
    private void OpenUrl()
    {
        if (OnlineMod.Url is not null) PlatformHelper.OpenFileOrUrl(OnlineMod.Url);
    }

    [RelayCommand]
    private async Task LoadCoverAsync(CancellationToken cancellationToken)
    {
        await LoadCoverAsync(true, cancellationToken);
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

    public async Task LoadCoverAsync(bool refresh = false, CancellationToken cancellationToken = default)
    {
        if (LocalMod is not null && File.Exists(LocalMod.InfoPicturePath))
        {
            Pic = await Task.Run(() => new Bitmap(LocalMod.InfoPicturePath), cancellationToken);
            return;
        }

        await using var imageStream = await OnlineMod.LoadPicBitmapAsync(refresh, cancellationToken);
        if (imageStream is not null && await IsValidImageFileAsync(imageStream))
        {
            Pic = await Task.Run(() => Bitmap.DecodeToWidth(imageStream, 400), cancellationToken);
        }
    }

    private static async Task<bool> IsValidImageFileAsync(Stream stream)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var bitmap = new Bitmap(stream);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                stream.Position = 0;
            }
        });
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ModViewModel vm) return false;
        return LocalMod == vm.LocalMod && OnlineMod == vm.OnlineMod;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(OnlineMod, LocalMod);
    }
}