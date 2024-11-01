﻿using Avalonia;
using Avalonia.Media;
using FluentIcons.Avalonia.Fluent;
using StarModsManager.Api;
using StarModsManager.Api.lib;

namespace StarModsManager.Common.Main;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .ExceptionHandler()
            .InitializeDataBackground()
            .With(CreateFontManagerOptions())
            .StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder InitializeDataBackground(this AppBuilder builder)
    {
        SMMDebug.AttachToParentConsole();
        SMMDebug.Info("Starting StarModsManager...");
        _ = Task.Run(async () => await ModsHelper.Instance.FindModsAsync());
        SMMDebug.Info($"Welcome to StarModsManager v{Services.AppVersion}");
        var startTime = DateTime.Now;
        SMMDebug.Info($"Starting at: {startTime:yyyy-MM-dd HH:mm:ss}");
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
        {
            return new FontManagerOptions
            {
                DefaultFamilyName = "Segoe UI",
                FontFallbacks =
                [
                    new FontFallback { FontFamily = "Microsoft YaHei" },
                    new FontFallback { FontFamily = "Arial" }
                ]
            };
        }
        else if (OperatingSystem.IsLinux())
        {
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
        }
        else if (OperatingSystem.IsMacOS())
        {
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
        }
        else
        {
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
}