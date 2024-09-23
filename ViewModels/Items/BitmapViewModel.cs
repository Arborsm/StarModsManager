using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StarModsManager.ViewModels.Items;

public partial class BitmapViewModel(Bitmap bitmap, string filePath) : ViewModelBase
{
    [ObservableProperty]
    private Bitmap _pic = bitmap;

    public string FilePath { get; } = filePath;
}