using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Serilog;
using Serilog.Events;
using StarModsManager.Assets;

namespace StarModsManager.Api;

public static class SMMDebug
{
    public static void Init()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
#if !DEBUG
            DebugHelper.InitConsole();
#endif
        }

        var debugFileName = Path.Combine(Services.LogDir, "debug_.log");
        var logFileName = Path.Combine(Services.LogDir, "log_.log");

        using var log = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .WriteTo.File(debugFileName,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 10,
                restrictedToMinimumLevel: LogEventLevel.Verbose)
            .WriteTo.File(logFileName,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 10,
                restrictedToMinimumLevel: LogEventLevel.Information)
            .CreateLogger();
        Log.Logger = log;

        var startTime = DateTime.Now;
        Log.Information("Starting StarModsManager...");
        Log.Information("Welcome to StarModsManager v{Version}", Services.AppVersion);
        Log.Information("Starting at: {Time}", startTime);
    }

    public static void Error(Exception e, string? msg = default, bool isMsg = true)
    {
        if (isMsg) Services.Notification.Show(Lang.ErrorOccurred, msg ?? e.Message, Severity.Error);
        Log.Error(e, msg ?? e.Message);
    }
}

[SupportedOSPlatform("windows")]
[SuppressMessage("ReSharper", "All")]
public static partial class DebugHelper
{
    private const int AttachParentProcess = -1;

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial void AttachConsole(int dwProcessId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllocConsole();

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GetConsoleWindow();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public static void ShowConsole()
    {
        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero) ShowWindow(handle, SW_SHOW);
    }

    public static void HideConsole()
    {
        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero) ShowWindow(handle, SW_HIDE);
    }

    public static void AttachToParentConsole()
    {
        AttachConsole(AttachParentProcess);
    }

    public static void InitConsole()
    {
        AllocConsole();
        HideConsole();

        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        Console.SetWindowSize(60, 25);
        // Console.SetBufferSize(60, 25);
    }
}