using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using StarModsManager.Common.Main;
using StarModsManager.Views;

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
        ServiceLocator.RegisterAllViewModels();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}