using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using StarModsManager.Common.Main;
using StarModsManager.Common.Mods;

namespace StarModsManager.ViewModels;

public partial class PicsSelectViewModel(OnlineMod mod) : ViewModelBase
{
    public ObservableCollection<Bitmap> Pics { get; } = [];

    [ObservableProperty]
    private bool _isLoading;
    [ObservableProperty] 
    private Bitmap? _selectedPic;

    public async Task LoadPicsAsync()
    {
        var url = mod.Url;
        if (url is not null) await LoadPicsAsync(url);
    }
    
    public async Task LoadPicsAsync(string modUrl)
    {
        var modLinks = await ModLinks.Instance.GetModPicsUrl(modUrl, ModLinks.Pics);
        await Task.WhenAll(modLinks.Select(async it =>
        {
            var cachePath = Path.Combine(Program.MainConfig.CachePath, mod.ModId);
            if (!Path.Exists(cachePath)) Directory.CreateDirectory(cachePath);
            var picFilePath = Path.Combine(cachePath, Path.GetFileNameWithoutExtension(it) + ".bmp");
            var picStream = await mod.LoadPicBitmapAsync(it, picFilePath);
            if (picStream is not null)
            {
                var pic = await Task.Run(() => Bitmap.DecodeToWidth(picStream, 400));
                Pics.Add(pic);
            }
        }));
    }
}