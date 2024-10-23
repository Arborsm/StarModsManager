using Avalonia;
using Avalonia.Threading;

namespace StarModsManager.Api.lib;

public static class GlobalExceptionHandler
{
    public static AppBuilder ExceptionHandler(this AppBuilder builder)
    {
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;
        //Dispatcher.UIThread.UnhandledException += OnUIThreadUnhandledException;
        return builder;
    }

    private static void OnTaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        StarDebug.Error(e.Exception);
    }

    private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception) StarDebug.Error(exception);
    }
}