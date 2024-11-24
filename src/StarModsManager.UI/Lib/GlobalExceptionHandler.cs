using Avalonia;
using Serilog;
using StarModsManager.Api;
using StarModsManager.Assets;

namespace StarModsManager.Lib;

public static class GlobalExceptionHandler
{
    public static AppBuilder ExceptionHandler(this AppBuilder builder)
    {
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;
        // Dispatcher.UIThread.UnhandledException += OnUIThreadUnhandledException;
        return builder;
    }

    private static void OnTaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "UnobservedTaskException");
        Services.Notification.Show(Lang.ThreadInternalError, e.Exception.Message, Severity.Error);
        e.SetObserved();
    }

    private static async void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception exception) Log.Fatal(exception, "UnhandledException");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "UnhandledException");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}