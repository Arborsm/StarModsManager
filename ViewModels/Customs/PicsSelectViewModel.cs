using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarModsManager.Common.Main;
using StarModsManager.Common.Mods;
using StarModsManager.ViewModels.Items;
using StarModsManager.Views;

namespace StarModsManager.ViewModels.Customs;

public partial class PicsSelectViewModel : ViewModelBase
{
    public ObservableCollection<BitmapViewModel> Pics { get; } = [];

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private BitmapViewModel? _selectedPic;

    private readonly ModViewModel _mod;

    // Test
    public PicsSelectViewModel()
    {
        _mod = new ModViewModel(new OnlineMod("", "", ""),
            new LocalMod("E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\mods\\[CP] H&W Bathroom Furniture"));
        Task.Run(LoadPicsAsync);
    }

    public PicsSelectViewModel(ModViewModel mod)
    {
        _mod = mod;
        Task.Run(LoadPicsAsync);
    }

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
        var modLinks = await ModLinks.Instance.GetModPicsUrl(modUrl, ModLinks.Pics);
        
        await Task.WhenAll(modLinks.Select(async it =>
        {
            var cachePath = Path.Combine(Program.MainConfig.CachePath, _mod.OnlineMod.ModId);
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
            foreach (var pic in pics)
            {
                Pics.Add(pic);
            }
        });
        
        IsLoading = false;
    }

    [RelayCommand]
    private async Task SelectPic()
    {
        if (SelectedPic is null) return;
        try
        {
            var picPath = _mod.LocalMod!.InfoPicturePath;
            await Task.Run(() => File.Copy(SelectedPic.FilePath, picPath, true));
            await _mod.LoadCover(TimeSpan.Zero, CancellationToken.None);
        }
        catch (Exception ex)
        {
            StarDebug.Error($"An error occurred: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CustomPic()
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

            var customPicPath = await TopLevel.GetTopLevel(MainWindow.Instance)!.StorageProvider
                .OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select a custom picture",
                    AllowMultiple = false,
                    FileTypeFilter = fileTypes
                });
            
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
                    StarDebug.Error("Image converted and saved successfully.");
                }
            }
            await _mod.LoadCover(TimeSpan.Zero, CancellationToken.None);
        }
        catch (Exception ex)
        {
            StarDebug.Error($"An error occurred: {ex.Message}");
        }
    }
}