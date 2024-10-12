using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Polly;
using Polly.Retry;
using StarModsManager.Common.Main;

namespace StarModsManager.Api;

/// <summary>
///     Handles HTTP requests with retry policy and batch execution.
/// </summary>
public class HttpBatchExecutor
{
    private const int InitialGroupSize = 20;
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

    /// <summary>
    ///     Gets the singleton instance of HttpBatchExecutor.
    /// </summary>
    public static HttpBatchExecutor Instance => LazyInstance.Value;

    /// <summary>
    ///     Executes tasks in batches with optional initial delay.
    /// </summary>
    /// <typeparam name="T">Type of the task's result.</typeparam>
    /// <param name="tasks">Enumerable of functions returning tasks to execute.</param>
    /// <param name="groupSize">Size of each batch group.</param>
    /// <param name="cancellationToken">Token to observe while waiting for the tasks to complete.</param>
    /// <returns>A task that represents the asynchronous operation, containing a list of results.</returns>
    public async Task<List<T>> ExecuteBatchAsync<T>(IEnumerable<Func<TimeSpan, CancellationToken, Task<T>>> tasks,
        int groupSize = InitialGroupSize, CancellationToken cancellationToken = default)
    {
        return await ExecuteInBatches(tasks, async (task, initialDelay, ct)
            => await task(initialDelay, ct), groupSize, cancellationToken);
    }

    /// <summary>
    ///     Executes tasks in batches with optional initial delay, without expecting a result.
    /// </summary>
    /// <param name="tasks">Enumerable of functions returning tasks to execute.</param>
    /// <param name="groupSize">Size of each batch group.</param>
    /// <param name="cancellationToken">Token to observe while waiting for the tasks to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteBatchAsync(IEnumerable<Func<TimeSpan, CancellationToken, Task>> tasks,
        int groupSize = InitialGroupSize, CancellationToken cancellationToken = default)
    {
        await ExecuteInBatches(tasks, async (task, initialDelay, ct) =>
        {
            await task(initialDelay, ct);
            return Task.CompletedTask;
        }, groupSize, cancellationToken);
    }

    /// <summary>
    ///     Executes tasks in specified batch groups with delay applied between tasks and groups.
    /// </summary>
    /// <typeparam name="TTask">Type of the tasks to be executed.</typeparam>
    /// <typeparam name="TResult">Type of the task result.</typeparam>
    /// <param name="tasks">Enumerable of tasks to execute.</param>
    /// <param name="executeTask">Function to execute each task with delay.</param>
    /// <param name="groupSize">Size of each batch group.</param>
    /// <param name="cancellationToken">Token to observe while waiting for the tasks to complete.</param>
    /// <returns>A task that represents the asynchronous operation, containing a list of results.</returns>
    private static async Task<List<TResult>> ExecuteInBatches<TTask, TResult>(
        IEnumerable<TTask> tasks,
        Func<TTask, TimeSpan, CancellationToken, Task<TResult>> executeTask,
        int groupSize,
        CancellationToken cancellationToken)
    {
        var results = new List<TResult>();
        var groups = tasks
            .Select((task, index) => new { task, index })
            .GroupBy(x => x.index / groupSize);

        var sw = Stopwatch.StartNew();
        foreach (var group in groups)
        {
            var groupTasks = group.Select(async x =>
            {
                var initialDelay = TimeSpan.FromSeconds(Jitter.NextDouble() * 2);
                return await executeTask(x.task, initialDelay, cancellationToken);
            });

            sw.Start();
            results.AddRange(await Task.WhenAll(groupTasks));
            sw.Stop();

            if (!(sw.ElapsedMilliseconds <= 3000))
                await Task.Delay(TimeSpan.FromSeconds(Jitter.NextDouble() * 3), cancellationToken);
        }
        StarDebug.Debug($"All tasks completed in {sw.Elapsed.TotalSeconds:0.00}s.");
        return results;
    }

    /// <summary>
    ///     Executes an HTTP GET request to the specified URI with retry policy.
    /// </summary>
    /// <param name="uri">Request URI.</param>
    /// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
    /// <returns>An HttpResponseMessage representing the response.</returns>
    public async Task<HttpResponseMessage> GetAsync(string uri, CancellationToken cancellationToken)
    {
        return await _policy.ExecuteAsync(async ct =>
            await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct), cancellationToken);
    }
}