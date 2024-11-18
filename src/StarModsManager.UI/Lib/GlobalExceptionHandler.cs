using Avalonia;
using Avalonia.Threading;
using Serilog;
using StarModsManager.Api;

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

    private static void OnUIThreadUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "UnhandledException");
        e.Handled = true;
    }

    private static void OnTaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "UnobservedTaskException");
        Services.Notification.Show("线程内错误", e.Exception.Message, Severity.Error);
        e.SetObserved();
    }

    private static async void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception exception) Log.Fatal(exception, "UnhandledException");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}