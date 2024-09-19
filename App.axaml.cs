using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
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

    private const int AttachParentProcess = -1;

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    /// <summary>
    ///     Redirects the console output of the current process to the parent process.
    /// </summary>
    /// <remarks>
    ///     Must be called before calls to <see cref="Console.WriteLine()" />.
    /// </remarks>
    public static void AttachToParentConsole()
    {
        AttachConsole(AttachParentProcess);
    }
    
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            AttachToParentConsole();
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}