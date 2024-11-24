using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StarModsManager.ViewModels.Items;

public partial class BitmapViewModel(Bitmap bitmap, string filePath) : ViewModelBase
{
    [ObservableProperty]
    public partial Bitmap Pic { get; set; } = bitmap;

    public string FilePath { get; } = filePath;
}