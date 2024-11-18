using System.Net;
using System.Net.Http.Headers;
using HtmlAgilityPack;
using Polly;
using Polly.Retry;
using Serilog;

namespace StarModsManager.Api;

public class HttpHelper
{
    private const int MaxConcurrentRequests = 10; // 设置最大并发请求数
    private static readonly Random Jitter = new();
    private static readonly Lazy<HttpHelper> LazyInstance = new(() => new HttpHelper());
    private readonly HttpClient _httpClient;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _policy;
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
                    var message = outcome.Result is not null
                        ? $"HTTP Status Code: {outcome.Result.StatusCode}"
                        : outcome.Exception?.Message;
                    Log.Verbose("Retry {retryAttempt} after {time}s delay due to: {message}",
                        retryAttempt, timespan.TotalSeconds.ToString("F"), message);
                }
            );
        _semaphore = new SemaphoreSlim(MaxConcurrentRequests, MaxConcurrentRequests);
    }

    public static HttpHelper Instance => LazyInstance.Value;

    public async Task<HttpResponseMessage> GetAsync(string uri, CancellationToken cancellationToken)
    {
        return await GetAsync(new Uri(uri), cancellationToken);
    }

    public async Task<HttpResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _policy.ExecuteAsync(async ct =>
                await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct), cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("The SSL connection could not be established",
                                                  StringComparison.OrdinalIgnoreCase))
        {
            Services.Notification.Show("网络链接错误, 请检查网络设置");
            return new HttpResponseMessage();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<HtmlDocument> FetchHtmlDocumentAsync(string url, CancellationToken cancellationToken = default)
    {
        var response = await GetAsync(url, cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);
        return htmlDoc;
    }
}