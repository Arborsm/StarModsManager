using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StarModsManager.Api;
using StarModsManager.Api.lib;
using StarModsManager.Api.NexusMods;
using StarModsManager.Common.Main;
using StarModsManager.ViewModels.Items;

namespace StarModsManager.ViewModels.Customs;

public partial class PicsSelectViewModel : ViewModelBase
{
    private readonly ModViewModel _mod;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListEmpty))]
    private bool _isLoading;

    [ObservableProperty]
    private BitmapViewModel? _selectedPic;

    // Test
    public PicsSelectViewModel() : this(new ModViewModel()) { }

    public PicsSelectViewModel(ModViewModel mod)
    {
        _mod = mod;
        _ = Task.Run(LoadPicsAsync);
        Pics.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsListEmpty));
    }

    public ObservableCollection<BitmapViewModel> Pics { get; } = [];
    public bool IsListEmpty => Pics.Count < 1 && !IsLoading;

    private async Task LoadPicsAsync()
    {
        try
        {
            var url = _mod.OnlineMod.Url;
            if (url is not null) await LoadPicsAsync(url);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private async Task LoadPicsAsync(string modUrl)
    {
        IsLoading = true;
        List<BitmapViewModel> pics = [];
        var nexusMod = await NexusMod.Create(modUrl);
        var modLinks = nexusMod.GetModPicsUrlAsync(NexusMod.Pics);

        await Task.WhenAll(modLinks.Select(async it =>
        {
            var cachePath = Path.Combine(Services.MainConfig.CachePath, _mod.OnlineMod.ModId);
            if (!Path.Exists(cachePath)) Directory.CreateDirectory(cachePath);
            var picFilePath = Path.Combine(cachePath, Path.GetFileNameWithoutExtension(it) + ".bmp");
            var picStream = await _mod.OnlineMod.LoadPicBitmapAsync(it, picFilePath);
            if (picStream is not null)
            {
                var pic = await Task.Run(() => Bitmap.DecodeToWidth(picStream, 400));
                pics.Add(new BitmapViewModel(pic, picFilePath));
            }
        }));

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            foreach (var pic in pics) Pics.Add(pic);
        });

        IsLoading = false;
    }
    
    [RelayCommand]
    private async Task SelectPicAsync(CancellationToken cancellationToken)
    {
        if (SelectedPic is null) return;
        try
        {
            var picPath = _mod.LocalMod!.InfoPicturePath;
            await Task.Run(() => File.Copy(SelectedPic.FilePath, picPath, true), cancellationToken);
            await _mod.LoadCoverAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            SMMDebug.Error($"An error occurred: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CustomPicAsync(CancellationToken cancellationToken)
    {
        try
        {
            var fileTypes = new FilePickerFileType[]
            {
                new("Image Files")
                {
                    Patterns = ["*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif"],
                    MimeTypes = ["image/jpeg", "image/png", "image/bmp", "image/gif"]
                }
            };

            var dialogMessage = new PickupFilesDialogMessage
            {
                Title = "Select a custom picture",
                FileTypeFilter = fileTypes
            };

            WeakReferenceMessenger.Default.Send(dialogMessage);

            var customPicPath = await dialogMessage.CompletionSource.Task;

            var picPath = _mod.LocalMod!.InfoPicturePath;
            if (customPicPath.Count > 0)
            {
                var sourcePath = customPicPath[0].TryGetLocalPath();
                if (sourcePath != null)
                {
                    await using (var sourceStream = new FileStream(sourcePath, FileMode.Open))
                    {
                        var bitmap = new Bitmap(sourceStream);
                        await using (var destinationStream = new FileStream(picPath, FileMode.Create))
                        {
                            bitmap.Save(destinationStream);
                        }
                    }

                    SMMDebug.Error("Image converted and saved successfully.");
                }
            }

            await _mod.LoadCoverAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            SMMDebug.Error($"An error occurred: {ex.Message}");
        }
    }
}