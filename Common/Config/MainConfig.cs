using CommunityToolkit.Mvvm.ComponentModel;
using StarModsManager.Common.Main;

namespace StarModsManager.Common.Config;

public partial class MainConfig : ConfigBase
{
    [ObservableProperty]
    private string _directoryPath = AppDomain.CurrentDomain.BaseDirectory;
    [ObservableProperty]
    private string _cachePath = Path.Combine(Services.AppSavingPath, "Cache");
    [ObservableProperty]
    private bool _debug;

    public MainConfig()
    {
        if (!Directory.Exists(CachePath)) Directory.CreateDirectory(CachePath);
    }
}