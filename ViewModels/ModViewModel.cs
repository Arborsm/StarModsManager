using System;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarModsManager.Models;

namespace StarModsManager.ViewModels;

public partial class ModViewModel(Mod mod) : ViewModelBase
{
    [ObservableProperty]
    private Bitmap? _pic;
    private string Url => mod.Url;
    private string Title => mod.Title;

    [RelayCommand]
    private void OpenUrl()
    {
        System.Diagnostics.Process.Start("explorer.exe", Url);
    }
    
    [RelayCommand]
    public async Task LoadCover(bool refresh = false)
    {
#if DEBUG
        if (refresh) Console.WriteLine(@"Refreshing cover");
#endif
        try
        {
            await using var imageStream = await mod.LoadPicBitmapAsync(refresh);
            Pic = await Task.Run(() => Bitmap.DecodeToWidth(imageStream, 400));
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public async Task SaveToDiskAsync()
    {
        await mod.SaveAsync();

        if (Pic != null)
        {
            var bitmap = Pic;

            await Task.Run(() =>
            {
                using var fs = mod.SavePicBitmapStream();
                bitmap.Save(fs);
            });
        }
    }
}