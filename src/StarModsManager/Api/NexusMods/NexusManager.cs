﻿using System.Net;
using System.Text.Json;
using Polly;
using RestEase;
using Serilog;
using StarModsManager.Api.NexusMods.Interface;
using StarModsManager.Api.NexusMods.Limit;
using StarModsManager.Api.NexusMods.Responses;

namespace StarModsManager.Api.NexusMods;

public static class NexusManager
{
    private static NexusApiClient _api = null!;
    private static Throttle _throttle = null!;
    public static bool IsAvailable { get; private set; }

    public static void Initialize(string apiKey, string userAgent)
    {
        _api = new NexusApiClient(apiKey, userAgent);
        _throttle = new Throttle(30, TimeSpan.FromSeconds(1));
        _ = Test();
    }

    public static async Task Test()
    {
        await Task.Run(Init).ContinueWith(_ => IsAvailable = !RateLimits.IsBlocked());
    }

    public static async Task<object?> Init()
    {
        var retryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        try
        {
            var result = await retryPolicy.ExecuteAsync(async () =>
            {
                try
                {
                    return await _api.ValidateUser();
                }
                catch (Exception e)
                {
                    if (e is ApiException { StatusCode: HttpStatusCode.Unauthorized })
                    {
                        Log.Warning("Invalid Api Key");
                        return null;
                    }

                    Log.Warning(e, "Error in Get Api Limits");
                    return null;
                }
            });

            await RateLimits.PrintRemaining();
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in Init");
            return null;
        }
    }

    public static async Task<ModInfo?> GetModAsync(int modId)
    {
        return await ExecuteThrottledRequestAsync(() => _api.GetMod(modId));
    }

    public static async Task<ModFileList?> GetModFilesAsync(int modId)
    {
        return await ExecuteThrottledRequestAsync(() => _api.GetModFiles(modId));
    }

    public static async Task<Uri?> GetPicsAsync(int modId)
    {
        return await GetModAsync(modId).ContinueWith(mod => mod.Result?.PictureUrl);
    }

    public static async Task<ModFileDownloadLink[]?> GetModFileDownloadLinkAsync(int modId, int fileId)
    {
        return await ExecuteThrottledRequestAsync(() => _api.GetModFileDownloadLink(modId, fileId));
    }

    public static async Task<Uri?> GetModFileAsync(int fileId)
    {
        return (await ExecuteThrottledRequestAsync(() => NexusWebClient.Instance.GetModDownloadUrl(fileId)))?.Url;
    }

    private static async Task<T?> ExecuteThrottledRequestAsync<T>(Func<Task<T?>> apiCall)
    {
        try
        {
            if (!RateLimits.IsBlocked()) return await _throttle.Queue(apiCall);
            var renewDelay = RateLimits.GetTimeUntilRenewal();
            if (renewDelay.TotalMilliseconds > 0) await Task.Delay(renewDelay);
            return await _throttle.Queue(apiCall);
        }
        catch (ApiException exception)
        {
            if (exception.StatusCode == HttpStatusCode.Forbidden) throw new NotPremiumException();
        }
        catch (JsonException ex)
        {
            Log.Error($"Deserialization error: {ex.Message}");
            Log.Error($"JSON: {ex.Source}");
        }
        catch (Exception e)
        {
            Log.Error(e, "Error in NexusManager");
        }

        return default;
    }
}

public class NotPremiumException : HttpRequestException;