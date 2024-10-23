using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using StarModsManager.ViewModels.Pages;
using MainWindow = StarModsManager.Views.MainWindow;

namespace StarModsManager;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        RequestedThemeVariant = ThemeVariant.Dark;
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        BindingPlugins.DataValidators.RemoveAt(0);
        var collection = new ServiceCollection()
            .AddSingleton<MainPageViewModel>()
            .AddSingleton<DownloadPageViewModel>()
            .AddSingleton<TransPageViewModel>()
            .AddSingleton<ProofreadPageViewModel>()
            .AddSingleton<SettingsPageViewModel>()
            .BuildServiceProvider();
        
        Ioc.Default.ConfigureServices(collection);
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}