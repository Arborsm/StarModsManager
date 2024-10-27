using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using NLog;
using StarModsManager.Api.lib;
using StarModsManager.Common.Main;

namespace StarModsManager.Api;

public static partial class SMMDebug
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial void AttachConsole(int dwProcessId);

    private const int AttachParentProcess = -1;

    /// <summary>
    ///     Redirects the console output of the current process to the parent process.
    /// </summary>
    /// <remarks>
    ///     Must be called before calls to <see cref="Console.WriteLine()" />.
    /// </remarks>
    internal static void AttachToParentConsole()
    {
        AttachConsole(AttachParentProcess);
    }

    static SMMDebug()
    {
        var debugFileName = Path.Combine(Services.LogDir, "debug_${shortdate}.log");
        var logFileName = Path.Combine(Services.LogDir, "log_${shortdate}.log");

        LogManager.Setup().LoadConfiguration(builder =>
        {
            builder.ForLogger().FilterMinLevel(LogLevel.Trace)
                .WriteToConsole("${longdate}|${level:uppercase=true}|${message}");
            builder.ForLogger().FilterMinLevel(LogLevel.Trace).WriteToFile(debugFileName,
                "${longdate}|${level:uppercase=true}|${message}", maxArchiveDays: 10);
            builder.ForLogger().FilterMinLevel(LogLevel.Info)
                .WriteToFile(logFileName, "${longdate}|${level:uppercase=true}|${message}");
        });
    }

    public static void Trace(string message, params object[] args)
    {
        Logger.Trace(message, args);
    }

    public static void Debug(string message, params object[] args)
    {
        Logger.Debug(message, args);
    }

    public static void Info(string message, params object[] args)
    {
        Logger.Info(message, args);
    }

    public static void Warning(string message, params object[] args)
    {
        Logger.Warn(message, args);
    }

    public static void Error(string message, params object[] args)
    {
        Logger.Error(message, args);
    }

    public static void Log(string message, LogLevel logLevel, params object[] args)
    {
        if (logLevel == LogLevel.Trace) Trace(message, args);
        else if (logLevel == LogLevel.Debug) Debug(message, args);
        else if (logLevel == LogLevel.Info) Info(message, args);
        else if (logLevel == LogLevel.Warn) Warning(message, args);
        else if (logLevel == LogLevel.Error) Error(message, args);
    }

    public static void Fatal(Exception e, string? message = default)
    {
        Logger.Fatal(e, message);
    }

    public static void Error(Exception e, string? msg = default, bool isMsg = true)
    {
        var finalMessage = new StringBuilder();
        finalMessage.Append(msg ?? e.Message);

        if (e.InnerException is not null) finalMessage.Append($" Inner Exception: {e.InnerException.Message}");

        if (isMsg)
        {
            WeakReferenceMessenger.Default.Send(new NotificationMessage
            {
                Title = "Error",
                Message = finalMessage.ToString(),
                Severity = InfoBarSeverity.Error
            });
        }

        Logger.Error(e, msg);
        Logger.Error(e.StackTrace);
        if (e.InnerException is not null) Logger.Error(e.InnerException.StackTrace);
    }
}