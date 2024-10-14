using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Polly;
using Polly.Retry;
using StarModsManager.Common.Main;

namespace StarModsManager.Api;

public class HttpBatchExecutor
{
    private static readonly Random Jitter = new();
    private static readonly Lazy<HttpBatchExecutor> LazyInstance = new(() => new HttpBatchExecutor());
    private readonly HttpClient _httpClient;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _policy;

    public HttpBatchExecutor()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36 Edg/128.0.0.0");
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        client.DefaultRequestHeaders.Add("Referer", "https://www.nexusmods.com/stardewvalley/mods/?BH=3");

        _httpClient = client;

        _policy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(msg =>
                msg.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.InternalServerError
                    or HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(
                6,
                retryAttempt =>
                {
                    var seconds = Math.Pow(2, retryAttempt);
                    var jitterSeconds = seconds * (0.5 + Jitter.NextDouble());
                    return TimeSpan.FromSeconds(jitterSeconds);
                },
                (outcome, timespan, retryAttempt, _) =>
                {
                    var message = outcome.Result is not null
                        ? $"HTTP Status Code: {outcome.Result.StatusCode}"
                        : outcome.Exception?.Message;
                    StarDebug.Trace(
                        $"Retry {retryAttempt} after {timespan.TotalSeconds:0.00}s delay due to: {message}");
                }
            );
    }
    
    public static HttpBatchExecutor Instance => LazyInstance.Value;
    
    public async Task<HttpResponseMessage> GetAsync(string uri, CancellationToken cancellationToken)
    {
        return await _policy.ExecuteAsync(async ct =>
            await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct), cancellationToken);
    }
}