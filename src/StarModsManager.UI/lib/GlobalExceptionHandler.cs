using Avalonia;
using StarModsManager.Api;

namespace StarModsManager.lib;

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
        SMMDebug.Error("UnobservedTaskException: ");
        e.Exception.InnerExceptions.ForEach(exception => SMMDebug.Error(exception, isMsg: false));
        e.SetObserved();
    }

    private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception) SMMDebug.Error(exception);
    }
}