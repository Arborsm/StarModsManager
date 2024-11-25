using System.Text.Json;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using StarModsManager.Api;
using StarModsManager.Api.NexusMods;
using StarModsManager.Api.SMAPI;
using StarModsManager.Assets;
using StarModsManager.Mods;
using StarModsManager.ViewModels.Customs;
using StarModsManager.Views.Customs;

namespace StarModsManager.ViewModels.Items;

public partial class ModViewModel : ViewModelBase
{
    private string? _localModPath;

    public ModViewModel(OnlineMod onlineMod, LocalMod? localMod = default)
    {
        OnlineMod = onlineMod;
        LocalMod = localMod;
        if (string.IsNullOrEmpty(onlineMod.ModId) && localMod == null)
        {
            Task.Run(OnlineMod.SaveAsync);
        }
        else if (localMod != null)
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDependencyMissing))]
    [NotifyPropertyChangedFor(nameof(IsDisabledOrDependencyMissing))]
    public partial bool IsDisabled { get; set; }

    [ObservableProperty]
    public partial Bitmap? Pic { get; set; }

    public bool IsDependencyMissing => IsLocal && LocalMod!.IsDependencyMissing && !IsDisabled;
    public bool IsDisabledOrDependencyMissing => IsDisabled || (IsLocal && IsDependencyMissing);

    public OnlineMod OnlineMod { get; }
    public LocalMod? LocalMod { get; }

    private bool IsLocal => LocalMod != null;

    [RelayCommand]
    private async Task DownloadAsync()
    {
        Services.Notification.Show(Lang.GetDownloadUrl);
        await new NexusDownload(OnlineMod.Url).GetModDownloadUrlAsync();
        Services.LifeCycle.ShowDownloadManager();
    }

    [RelayCommand]
    private async Task GetDetailAsync()
    {
        ModDetailView? content;
        if (LocalMod != null)
        {
            var manifest = await File.ReadAllTextAsync(Path.Combine(LocalMod!.PathS, "manifest.json"));
            var mod = JsonSerializer.Deserialize(manifest, ManifestContent.Default.Manifest)!;
            content = new ModDetailView
            {
                DataContext = new ModDetailViewModel(mod, LocalMod!.MissingDependencies)
            };
        }
        else
        {
            await OnlineMod.LoadDetailAsync();
            content = new ModDetailView
            {
                DataContext = new ModDetailViewModel(OnlineMod)
            };
        }

        var flyout = new Flyout
        {
            Content = content,
            Placement = PlacementMode.Center
        };

        content.StackPanel.LostFocus += (_, _) => flyout.Hide();

        Services.PopUp.ShowFlyout(flyout);
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
    private void OpenModFolder()
    {
        PlatformHelper.OpenFileOrUrl(LocalMod!.PathS);
    }

    [RelayCommand]
    private void OpenUrl()
    {
        PlatformHelper.OpenFileOrUrl(OnlineMod.Url);
    }

    [RelayCommand]
    private async Task LoadCoverAsync(CancellationToken cancellationToken)
    {
        await LoadCoverAsync(true, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(IsLocal))]
    private void SwitchMod()
    {
        IsDisabled = ToggleDotPrefix();
    }

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
        if (LocalMod != null && File.Exists(LocalMod.InfoPicturePath))
        {
            if (refresh)
            {
                File.Delete(LocalMod.InfoPicturePath);
            }
            else
            {
                await using var stream = File.OpenRead(LocalMod.InfoPicturePath);
                Pic = await Task.Run(() => Bitmap.DecodeToWidth(stream, 400), cancellationToken);
                return;
            }
        }

        await using var imageStream = await OnlineMod.LoadPicBitmapAsync(refresh, cancellationToken);
        if (imageStream != null && await IsValidImageFileAsync(imageStream))
        {
            Pic = await Task.Run(() => Bitmap.DecodeToWidth(imageStream, 400), cancellationToken);
            if (LocalMod != null)
            {
                imageStream.Position = 0;
                var file = File.Create(LocalMod.InfoPicturePath);
                await imageStream.CopyToAsync(file, cancellationToken);
            }
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