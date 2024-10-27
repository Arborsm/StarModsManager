using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StarModsManager.Api;

public static class PlatformHelper
{
    public static void OpenFileOrUrl(string path)
    {
        try
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out var uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)) OpenUrl(path);
            else OpenFolder(path);
        }
        catch (Exception? e)
        {
            SMMDebug.Error(e);
        }
    }

    private static void OpenUrl(string url)
    {
        var psi = new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        };
        Process.Start(psi);
    }

    private static void OpenFolder(string path)
    {
        var psi = new ProcessStartInfo
        {
            FileName = GetFileNameForPlatform(),
            Arguments = path,
            UseShellExecute = true
        };
        Process.Start(psi);
    }

    private static string GetFileNameForPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "explorer.exe";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "xdg-open";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "open";
        }

        throw new PlatformNotSupportedException("Unsupported operating system.");
    }
}