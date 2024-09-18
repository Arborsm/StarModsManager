using StarModsManager.Common.Main;

namespace StarModsManager.Common.Config;

public class MainConfig
{
    public MainConfig()
    {
        if (!Directory.Exists(CachePath)) Directory.CreateDirectory(CachePath);
    }

    public string DirectoryPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
    public string CachePath { get; set; } = Path.Combine(Program.AppSavingPath, "Cache");
    public bool Debug { get; set; } = false;
}