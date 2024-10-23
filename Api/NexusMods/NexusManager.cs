using StarModsManager.Api.NexusMods.Interface;
using StarModsManager.Api.NexusMods.Limit;
using StarModsManager.Api.NexusMods.Responses;

namespace StarModsManager.Api.NexusMods;

public static class NexusManager
{
    private static NexusApiClient _api = null!;
    private static Throttle _throttle = null!;

    public static void Initialize(string apiKey, string userAgent)
    {
        _api = new NexusApiClient(apiKey, userAgent);
        _throttle = new Throttle(30, TimeSpan.FromSeconds(1));
        Task.Run(async () => await _api.ValidateUser());
    }
    
    public static async Task<ModInfo> GetModAsync(int modId)
    {
        return await ExecuteThrottledRequestAsync(() => _api.GetMod(modId));
    }

    public static async Task<ModFileList> GetModFilesAsync(int modId)
    {
        return await ExecuteThrottledRequestAsync(() => _api.GetModFiles(modId));
    }

    public static async Task<ModFileDownloadLink> GetModFileDownloadLinkAsync(int modId, int fileId)
    {
        return await ExecuteThrottledRequestAsync(() => _api.GetModFileDownloadLink(modId, fileId));
    }

    private static async Task<T> ExecuteThrottledRequestAsync<T>(Func<Task<T>> apiCall)
    {
        if (!RateLimits.IsBlocked()) return await _throttle.Queue(apiCall);
        var renewDelay = RateLimits.GetTimeUntilRenewal();
        if (renewDelay.TotalMilliseconds > 0) await Task.Delay(renewDelay);

        return await _throttle.Queue(apiCall);
    }
}