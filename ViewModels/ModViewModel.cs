using System;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using StarModsManager.Models;
using ReactiveUI;

namespace StarModsManager.ViewModels;

public class ModViewModel(Mod mod) : ViewModelBase
{
    private Bitmap? _pic;
    
    public  Bitmap? Pic
    {
        get => _pic;
        set => this.RaiseAndSetIfChanged(ref _pic, value);
    }
    public string Url => mod.Url;
    public string Title => mod.Title;

    public void OpenUrl()
    {
        System.Diagnostics.Process.Start("explorer.exe", Url);
    }
    
    public async Task LoadCover(bool refresh = false)
    {
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