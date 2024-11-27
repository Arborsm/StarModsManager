using Semver;
using Serilog;
using StarModsManager.Assets;

namespace StarModsManager.Api.NexusMods;

public class NexusDownload
{
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private volatile Task? _initializationTask;
    private volatile bool _isNoCookie;
    private volatile bool _isNotPremium;
    public static NexusDownload Instance { get; } = new();

    private async Task EnsureInitializedAsync()
    {
        var initTask = _initializationTask;
        if (initTask != null)
        {
            await initTask;
            return;
        }

        await _initLock.WaitAsync();
        try
        {
            initTask = _initializationTask;
            if (initTask != null)
            {
                await initTask;
                return;
            }

            _initializationTask = TestCookie();
            await _initializationTask;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public void Reset()
    {
        _isNotPremium = false;
        _initializationTask = null;
    }

    private async Task TestCookie()
    {
        try
        {
            const int fileId = 113065;
            var result = await Task.Run(async () => await NexusManager.GetModFileAsync(fileId));
            _isNoCookie = result is null;
            Log.Information("Testing Cookie, IsNoCookie: {isNoCookie}", _isNoCookie);
        }
        catch (Exception)
        {
            _isNoCookie = true;
            // ignored
        }
    }

    public async Task<string?> GetModDownloadUrlAsync(string? modUrl, SemVersion? version = null)
    {
        await EnsureInitializedAsync();

        var modId = int.TryParse(modUrl?.Split('/').Last(), out var result) ? result : -1;
        try
        {
            if (modId < 1) return null;
            var mods = await NexusManager.GetModFilesAsync(modId);
            if (mods is null) return null;
            var fileId = version is null
                ? mods.Files.First(x => x.Version == mods.Files.Max(file => file.Version)).FileId
                : mods.Files.First(x => x.Version == version.ToString()).FileId;

            string? link = null;
            if (!_isNoCookie) link = (await NexusManager.GetModFileAsync(fileId))?.ToString();

            if (!_isNotPremium)
            {
                try
                {
                    link ??= (await NexusManager.GetModFileDownloadLinkAsync(modId, fileId))?[0].Uri;
                }
                catch (NotPremiumException)
                {
                    Services.Notification.Show(Lang.Warning, Lang.NexusPremiumRequired, Severity.Warning);
                    _isNotPremium = true;
                }
            }

            if (link is null && (!_isNoCookie || !_isNotPremium))
            {
                Services.Notification.Show(Lang.Warning, Lang.ModFileNotFound, Severity.Warning);
            }

            return link;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to Download Mod: {modId}", modId);
            return null;
        }
    }

    public static async Task DownloadLatestModAsync(string modId, SemVersion? version = null)
    {
        var link = await Instance.GetModDownloadUrlAsync(modId, version);
        if (link is null) return;
        Services.LifeCycle.AddDownload(link);
    }
}