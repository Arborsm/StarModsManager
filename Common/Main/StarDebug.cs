using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using NLog;
using StarModsManager.Api;

namespace StarModsManager.Common.Main;

public static partial class StarDebug
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
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AttachConsole(AttachParentProcess);
        }
    }
    
    static StarDebug()
    {
        var logDirectory = Services.LogDir;
        Directory.CreateDirectory(logDirectory);
        var fileName = Path.Combine(logDirectory, "log_${shortdate}.log");
        if (File.Exists(fileName)) fileName = Rename(fileName);
        LogManager.Setup().LoadConfiguration(builder =>
        {
            builder.ForLogger().FilterMinLevel(LogLevel.Trace).WriteToConsole();
            builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToFile(fileName);
        });
    }

    private static string Rename(string fileName)
    {
        var i = 0;
        do 
        {
            fileName = Path.Combine(Path.GetDirectoryName(fileName)!, $"log_{i++}.log");
        } while (File.Exists(fileName));
        return fileName;
    }

    public static void Trace(string message, params object[] args)
    {
        Logger.Trace(message, args);
    }

    public static void Debug(string message, params object[] args)
    {
#if DEBUG
        Logger.Debug(message, args);
#endif
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

    public static void Fatal(Exception e,string? message = default)
    {
        Logger.Fatal(e, message);
    }

    public static void Error(Exception e, string? msg = default)
    {
        WeakReferenceMessenger.Default.Send(new NotificationMessage
        {
            Title = "Error",
            Message = msg ?? e.Message,
            Severity = InfoBarSeverity.Error
        });
        Logger.Error(e, msg);
    }
}