using System.Net;
using System.Net.Http.Headers;
using HtmlAgilityPack;
using Polly;
using Polly.Retry;
using Serilog;
using StarModsManager.Assets;

namespace StarModsManager.Api;

public class HttpHelper
{
    private const int MaxConcurrentRequests = 10;
    private static readonly Random Jitter = new();
    private static readonly Lazy<HttpHelper> LazyInstance = new(() => new HttpHelper());
    private readonly HttpClient _httpClient;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _policy;
    private readonly RateLimiter _rateLimiter = new();
    private readonly SemaphoreSlim _semaphore;

    private HttpHelper(string referer = "https://www.nexusmods.com/stardewvalley/mods/")
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(60);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36 Edg/128.0.0.0");
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        client.DefaultRequestHeaders.Add("Referer", referer);

        _httpClient = client;
        _policy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>(ex =>
                !ex.Message.Contains("The SSL connection could not be established", StringComparison.OrdinalIgnoreCase))
            .OrResult(msg =>
                msg.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.InternalServerError
                    or HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(
                6,
                retryAttempt =>
                {
                    var seconds = Math.Pow(2, retryAttempt);
                    var jitterSeconds = 10 + seconds * (0.5 + Jitter.NextDouble());
                    return TimeSpan.FromSeconds(jitterSeconds);
                },
                (outcome, timespan, retryAttempt, _) =>
                {
                    var message = outcome.Result != null
                        ? $"HTTP Status Code: {outcome.Result.StatusCode}"
                        : outcome.Exception?.Message;
                    Log.Verbose("Retry {retryAttempt} after {time}s delay due to: {message}",
                        retryAttempt, timespan.TotalSeconds.ToString("F"), message);
                }
            );
        _semaphore = new SemaphoreSlim(MaxConcurrentRequests, MaxConcurrentRequests);
    }

    public static HttpHelper Instance => LazyInstance.Value;

    public async Task<HttpResponseMessage> GetAsync(string uri, RequestPriority priority, CancellationToken ct)
    {
        return await GetAsync(new Uri(uri), ct, priority);
    }

    public async Task<HttpResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken,
        RequestPriority priority = RequestPriority.Normal)
    {
        await _rateLimiter.WaitAsync(priority);

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _policy.ExecuteAsync(async ct =>
                    await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct),
                cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return new HttpResponseMessage();
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("The SSL connection could not be established",
                                                  StringComparison.OrdinalIgnoreCase))
        {
            Services.Notification?.Show(Lang.NetworkErrorMsg);
            return new HttpResponseMessage();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<HtmlDocument> FetchHtmlDocumentAsync(string url, RequestPriority priority,
        CancellationToken ct = default)
    {
        var response = await GetAsync(url, priority, ct);
        var html = await response.Content.ReadAsStringAsync(ct);
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);
        return htmlDoc;
    }
}

public class RateLimiter : IDisposable
{
    private readonly Dictionary<RequestPriority, PriorityLimitInfo> _limitInfos;
    private readonly Lock _lock = new();
    private readonly Timer _timer;
    private long _sequenceNumber;

    public RateLimiter()
    {
        _limitInfos = new Dictionary<RequestPriority, PriorityLimitInfo>
        {
            [RequestPriority.High] = new(2, TimeSpan.FromSeconds(1)),
            [RequestPriority.Normal] = new(3, TimeSpan.FromSeconds(1)),
            [RequestPriority.Low] = new(3, TimeSpan.FromSeconds(1))
        };

        var minInterval = _limitInfos.Values.Min(x => x.Interval);
        _timer = new Timer(CheckWaitingTasks, null, minInterval, minInterval);
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void CheckWaitingTasks(object? state)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;

            foreach (var (_, info) in _limitInfos)
            {
                while (info.RequestTimestamps.Count > 0 &&
                       now - info.RequestTimestamps.Peek() > info.Interval)
                    info.RequestTimestamps.Dequeue();

                while (info.RequestTimestamps.Count < info.Limit &&
                       info.WaitingTasks.Count > 0)
                {
                    var tcs = info.WaitingTasks.Dequeue();
                    info.RequestTimestamps.Enqueue(now, 0);
                    tcs.TrySetResult(true);
                }
            }
        }
    }

    public Task WaitAsync(RequestPriority priority)
    {
        if (!_limitInfos.TryGetValue(priority, out var info))
            throw new ArgumentException($"Unsupported priority: {priority}");

        lock (_lock)
        {
            var now = DateTime.UtcNow;

            // 移除过期的时间戳
            while (info.RequestTimestamps.Count > 0 &&
                   now - info.RequestTimestamps.Peek() > info.Interval)
                info.RequestTimestamps.Dequeue();

            if (info.RequestTimestamps.Count < info.Limit)
            {
                info.RequestTimestamps.Enqueue(now, 0);
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>();
            var sequence = Interlocked.Increment(ref _sequenceNumber);
            info.WaitingTasks.Enqueue(tcs, sequence);
            return tcs.Task;
        }
    }

    private class PriorityLimitInfo(int limit, TimeSpan interval)
    {
        public int Limit { get; } = limit;
        public TimeSpan Interval { get; } = interval;
        public PriorityQueue<DateTime, int> RequestTimestamps { get; } = new();
        public PriorityQueue<TaskCompletionSource<bool>, long> WaitingTasks { get; } = new();
    }
}

public enum RequestPriority
{
    Low = 0,
    Normal = 1,
    High = 2
}