using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Serilog;
using StarModsManager.Assets;

namespace StarModsManager.Api.AI;

public class OllamaChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string>? Images { get; set; }
    public List<ToolCall>? ToolCalls { get; set; }
}

public class ToolCall
{
    public Function Function { get; set; } = new();
}

public class Function
{
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
}

public class OllamaChatResponse
{
    public string Model { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public OllamaChatMessage Message { get; set; } = new();
    public bool Done { get; set; }
    public string? DoneReason { get; set; }
    public long TotalDuration { get; set; }
    public long LoadDuration { get; set; }
    public long PromptEvalCount { get; set; }
    public long PromptEvalDuration { get; set; }
    public long EvalCount { get; set; }
    public long EvalDuration { get; set; }
}

public class OllamaChatRequest
{
    public string Model { get; set; } = string.Empty;
    public List<OllamaChatMessage> Messages { get; set; } = [];
    public bool Stream { get; set; }
    public string? Format { get; set; }
    public Dictionary<string, object>? Options { get; set; }
    public List<Tool>? Tools { get; set; }
    public TimeSpan? KeepAlive { get; set; }

    public void AddMessage(string role, string content)
    {
        Messages.Add(new OllamaChatMessage { Role = role, Content = content });
    }
}

public class Tool
{
    public string Type { get; set; } = string.Empty;
    public Function Function { get; set; } = new();
}

public class OllamaModelResponse
{
    public List<ModelInfo> Models { get; set; } = [];
}

public class ModelInfo
{
    public string Name { get; set; } = string.Empty;
    public ulong Size { get; set; }
    public DateTime Modified { get; set; }
    public string Digest { get; set; } = string.Empty;
}

public class OllamaService(string model, string server)
{
    private readonly string _server = NormalizeServer(server);

    private static string NormalizeServer(string server)
    {
        if (string.IsNullOrWhiteSpace(server))
            throw new ArgumentException(@"Server URL cannot be empty", nameof(server));

        server = server.Trim();
        if (!server.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !server.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            server = "http://" + server;

        return server.TrimEnd('/');
    }

    public async Task<OllamaChatResponse?> ChatAsync(string prompt, string msg, CancellationToken cancellation)
    {
        var chat = new OllamaChatRequest { Model = model };
        chat.AddMessage("system", prompt);
        chat.AddMessage("user", msg);

        return await ExecuteRequestAsync(
            async client =>
            {
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(chat, OllamaContent.Default.OllamaChatRequest),
                    Encoding.UTF8,
                    "application/json"
                );
                return await client.PostAsync($"{_server}/api/chat", jsonContent, cancellation);
            },
            OllamaContent.Default.OllamaChatResponse,
            cancellation
        );
    }

    public async Task<OllamaModelResponse?> GetSupportModelsAsync(CancellationToken cancellation)
    {
        return await ExecuteRequestAsync(
            client => client.GetAsync($"{_server}/api/tags", cancellation),
            OllamaContent.Default.OllamaModelResponse,
            cancellation
        );
    }

    private static HttpClient CreateHttpClient()
    {
        return new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    private static async Task<T?> ExecuteRequestAsync<T>(
        Func<HttpClient, Task<HttpResponseMessage>> sendRequest,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellation)
    {
        try
        {
            using var client = CreateHttpClient();
            var response = await sendRequest(client);
            var body = await response.Content.ReadAsStringAsync(cancellation);

            if (!response.IsSuccessStatusCode)
                throw new Exception(
                    $"Ollama service returns error code {response.StatusCode}. Body: {body ?? string.Empty}");

            return JsonSerializer.Deserialize(body, jsonTypeInfo);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to call Ollama service");
            Services.Notification?.Show(Lang.Warning, ex.Message, Severity.Warning, TimeSpan.FromSeconds(8));
            return default;
        }
    }
}

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    IgnoreReadOnlyFields = true,
    IgnoreReadOnlyProperties = true,
    Converters = [typeof(DateTimeConverter), typeof(TimeSpanConverter)]
)]
[JsonSerializable(typeof(OllamaChatRequest))]
[JsonSerializable(typeof(OllamaChatResponse))]
[JsonSerializable(typeof(OllamaModelResponse))]
[JsonSerializable(typeof(ModelInfo))]
[JsonSerializable(typeof(OllamaChatMessage))]
[JsonSerializable(typeof(List<OllamaChatMessage>))]
[JsonSerializable(typeof(ToolCall))]
[JsonSerializable(typeof(Function))]
[JsonSerializable(typeof(Tool))]
[JsonSerializable(typeof(List<Tool>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
public partial class OllamaContent : JsonSerializerContext;