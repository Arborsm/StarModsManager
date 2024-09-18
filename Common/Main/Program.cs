using Avalonia;
using StarModsManager.Common.Config;

namespace StarModsManager.Common.Main;

internal static class Program
{
    public static readonly string AppSavingPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StarModsManager");

    public static readonly MainConfig MainConfig = ConfigManager<MainConfig>.Load();
    public static readonly TransConfig TransConfig = ConfigManager<TransConfig>.Load();

    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}