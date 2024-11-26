using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using StarModsManager.Api;
using StarModsManager.Api.NexusMods;
using StarModsManager.ViewModels.Items;

namespace StarModsManager.ViewModels.Customs;

public partial class PicsSelectViewModel : ViewModelBase, IDisposable
{
    private readonly ModViewModel _mod;

    // Test
    public PicsSelectViewModel() : this(new ModViewModel())
    {
    }

    public PicsSelectViewModel(ModViewModel mod)
    {
        _mod = mod;
        Task.Run(LoadPicsAsync);
        Pics.CollectionChanged += OnPicsOnCollectionChanged;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListEmpty))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial BitmapViewModel? SelectedPic { get; set; }

    public ObservableCollection<BitmapViewModel> Pics { get; } = [];
    public bool IsListEmpty => Pics.Count < 1 && !IsLoading;

    public void Dispose()
    {
        Pics.CollectionChanged -= OnPicsOnCollectionChanged;
        GC.SuppressFinalize(this);
    }

    private void OnPicsOnCollectionChanged(object? o, NotifyCollectionChangedEventArgs args)
    {
        OnPropertyChanged(nameof(IsListEmpty));
    }

    private async Task LoadPicsAsync()
    {
        try
        {
            var url = _mod.OnlineMod.Url;
            await LoadPicsAsync(url);
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
        var nexusMod = await NexusMod.CreateAsync(modUrl);
        var modLinks = nexusMod.GetModPicsUrl(NexusMod.Pics);

        await Task.WhenAll(modLinks.Select(async it =>
        {
            var cachePath = Path.Combine(Services.MainConfig.CachePath, _mod.OnlineMod.ModId);
            if (!Path.Exists(cachePath)) Directory.CreateDirectory(cachePath);
            var picFilePath = Path.Combine(cachePath, Path.GetFileNameWithoutExtension(it) + ".bmp");
            var picStream = await _mod.OnlineMod.LoadPicBitmapAsync(it, picFilePath);
            if (picStream != null)
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
            Log.Error(ex, "An error occurred");
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

            var customPicPath =
                await Services.Dialog.ShowPickupFilesDialogAsync("Select a picture", false, fileTypes);

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

                    Log.Verbose("Image converted and saved successfully.");
                }
            }

            await _mod.LoadCoverAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred");
        }
    }
}