using System.IO.Pipes;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace StarModsManager.Lib;

public sealed class SingleInstanceManager : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Mutex _mutex;
    private readonly bool _ownsMutex;
    private readonly string _pipeName;
    private bool _disposed;

    public SingleInstanceManager(string appName)
    {
        _pipeName = $"{appName}Pipe";
        var mutexName = $"{appName}Mutex";
        _mutex = new Mutex(true, mutexName, out _ownsMutex);
        IsFirstInstance = _ownsMutex;

        if (IsFirstInstance) Task.Run(ListenForOtherInstances, _cts.Token);
    }

    public bool IsFirstInstance { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void NotifyFirstInstance()
    {
        using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
        try
        {
            client.Connect(1000);
            using var writer = new StreamWriter(client);
            writer.WriteLine("ACTIVATE");
            writer.Flush();
        }
        catch (TimeoutException)
        {
            // Ignore
        }
    }

    private async Task ListenForOtherInstances()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            await using var server = new NamedPipeServerStream(_pipeName, PipeDirection.In);
            await server.WaitForConnectionAsync(_cts.Token);
            using var reader = new StreamReader(server);
            var command = await reader.ReadLineAsync();
            if (command == "ACTIVATE") Dispatcher.UIThread.Post(ActivateMainWindow);
        }
    }

    private static void ActivateMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        var mainWindow = desktop.MainWindow;
        if (mainWindow != null)
        {
            mainWindow.WindowState = WindowState.Minimized;
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        }
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _cts.Cancel();
            if (_ownsMutex) _mutex.ReleaseMutex();
            _mutex.Dispose();
        }

        _disposed = true;
    }

    ~SingleInstanceManager()
    {
        Dispose(false);
    }
}