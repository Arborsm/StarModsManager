using Avalonia;
using Avalonia.Media;
using FluentIcons.Avalonia.Fluent;
using Serilog;
using StarModsManager.Api;
using StarModsManager.Lib;
using StarModsManager.ViewModels.Pages;

namespace StarModsManager;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using var manager = new SingleInstanceManager(Services.AppName);
        if (manager.IsFirstInstance)
        {
            BuildAvaloniaApp()
                .ExceptionHandler()
                .InitializeDataBackground()
                .With(CreateFontManagerOptions())
                .StartWithClassicDesktopLifetime(args);
        }
        else
        {
            manager.NotifyFirstInstance();
        }
    }

    private static AppBuilder InitializeDataBackground(this AppBuilder builder)
    {
        SMMDebug.Init();
        Log.Information("Starting StarModsManager...");
        _ = Task.Run(async () =>
        {
            await ViewModelService.Resolve<MainPageViewModel>().LoadModsAsync();
            if (Services.MainConfig.AutoCheckUpdates) ViewModelService.Resolve<UpdatePageViewModel>().Init();
        });
        Log.Information("Welcome to StarModsManager v{Version}", Services.AppVersion);
        var startTime = DateTime.Now;
        Log.Information("Starting at: {Time}", startTime);
        return builder;
    }

    private static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .UseSegoeMetrics()
            .LogToTrace();

    private static FontManagerOptions CreateFontManagerOptions()
    {
        if (OperatingSystem.IsWindows())
            return new FontManagerOptions
            {
                DefaultFamilyName = "Segoe UI",
                FontFallbacks =
                [
                    new FontFallback { FontFamily = "Microsoft YaHei" },
                    new FontFallback { FontFamily = "Arial" }
                ]
            };
        else if (OperatingSystem.IsLinux())
            return new FontManagerOptions
            {
                DefaultFamilyName = "DejaVu Sans",
                FontFallbacks =
                [
                    new FontFallback { FontFamily = "Liberation Sans" },
                    new FontFallback { FontFamily = "Microsoft YaHei" },
                    new FontFallback { FontFamily = "Arial" }
                ]
            };
        else if (OperatingSystem.IsMacOS())
            return new FontManagerOptions
            {
                DefaultFamilyName = "Helvetica",
                FontFallbacks =
                [
                    new FontFallback { FontFamily = "Helvetica Neue" },
                    new FontFallback { FontFamily = "Arial" },
                    new FontFallback { FontFamily = "Geneva" },
                    new FontFallback { FontFamily = "Lucida Grande" }
                ]
            };
        else
            return new FontManagerOptions
            {
                DefaultFamilyName = "Arial",
                FontFallbacks =
                [
                    new FontFallback { FontFamily = "DejaVu Sans" },
                    new FontFallback { FontFamily = "Liberation Sans" }
                ]
            };
    }
}