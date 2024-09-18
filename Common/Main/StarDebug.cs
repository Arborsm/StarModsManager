using NLog;

namespace StarModsManager.Common.Main;

public static class StarDebug
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static StarDebug()
    {
        var logDirectory = Path.Combine(Program.AppSavingPath, "Log");
        Directory.CreateDirectory(logDirectory);
        var fileName = Path.Combine(logDirectory, "log_${shortdate}.log");
        LogManager.Setup().LoadConfiguration(builder =>
        {
            builder.ForLogger().FilterMinLevel(LogLevel.Trace).WriteToConsole();
            builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToFile(fileName);
        });
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
        if      (logLevel == LogLevel.Trace) Trace(message, args);
        else if (logLevel == LogLevel.Debug) Debug(message, args);
        else if (logLevel == LogLevel.Info)  Info(message, args);
        else if (logLevel == LogLevel.Warn)  Warning(message, args);
        else if (logLevel == LogLevel.Error) Error(message, args);
    }
}