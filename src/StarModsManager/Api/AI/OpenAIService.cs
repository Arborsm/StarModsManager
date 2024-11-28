using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Serilog;
using StarModsManager.Assets;

namespace StarModsManager.Api.AI;

public class OpenAIChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class OpenAIChatChoice
{
    public int Index { get; set; }
    public OpenAIChatMessage Message { get; set; } = new();
}

public class OpenAIChatResponse
{
    public List<OpenAIChatChoice> Choices { get; set; } = [];
}

public class OpenAIChatRequest
{
    public string Model { get; set; } = string.Empty;
    public List<OpenAIChatMessage> Messages { get; set; } = [];

    public void AddMessage(string role, string content)
    {
        Messages.Add(new OpenAIChatMessage { Role = role, Content = content });
    }
}

public class OpenAIModelsResponse
{
    public List<ModelData> Data { get; set; } = new();
    public string Object { get; set; } = string.Empty;
}

public class ModelData
{
    public string Id { get; set; } = string.Empty;
    public string Object { get; set; } = string.Empty;
    public long Created { get; set; }
    public string OwnedBy { get; set; } = string.Empty;
    public List<ModelPermission>? Permission { get; set; }
    public string Root { get; set; } = string.Empty;
    public string? Parent { get; set; }
}

public class ModelPermission
{
    public string Id { get; set; } = string.Empty;
    public string Object { get; set; } = string.Empty;
    public long Created { get; set; }
    public bool AllowCreateEngine { get; set; }
    public bool AllowSampling { get; set; }
    public bool AllowLogprobs { get; set; }
    public bool AllowSearchIndices { get; set; }
    public bool AllowView { get; set; }
    public bool AllowFineTuning { get; set; }
    public string Organization { get; set; } = string.Empty;
    public string? Group { get; set; }
    public bool IsBlocking { get; set; }
}

public class OpenAIService(string model, string apiKey, string server)
{
    private readonly string _server = NormalizeServer(server);

    private static string NormalizeServer(string server)
    {
        if (string.IsNullOrWhiteSpace(server))
            throw new ArgumentException(@"Server URL cannot be empty", nameof(server));

        server = server.Trim();

        if (!server.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !server.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            server = "https://" + server;

        server = server.TrimEnd('/');

        if (!server.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)) server += "/v1";

        return server;
    }

    public async Task<OpenAIChatResponse?> ChatAsync(string prompt, string msg, CancellationToken cancellation)
    {
        var chat = new OpenAIChatRequest { Model = model };
        chat.AddMessage("system", prompt);
        chat.AddMessage("user", msg);

        return await ExecuteRequestAsync<OpenAIChatResponse>(
            async client =>
            {
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(chat, OpenAIContent.Default.OpenAIChatRequest),
                    Encoding.UTF8,
                    "application/json"
                );
                return await client.PostAsync($"{_server}/chat/completions", jsonContent, cancellation);
            },
            OpenAIContent.Default.OpenAIChatResponse,
            cancellation
        );
    }

    public async Task<OpenAIModelsResponse?> GetSupportModelsAsync(CancellationToken cancellation)
    {
        return await ExecuteRequestAsync<OpenAIModelsResponse>(
            client => client.GetAsync($"{_server}/models", cancellation),
            OpenAIContent.Default.OpenAIModelsResponse, cancellation);
    }

    private HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        if (string.IsNullOrEmpty(apiKey)) return client;
        if (_server.Contains("openai.azure.com/", StringComparison.Ordinal))
            client.DefaultRequestHeaders.Add("api-key", apiKey);
        else
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        return client;
    }

    private async Task<T?> ExecuteRequestAsync<T>(Func<HttpClient, Task<HttpResponseMessage>> sendRequest,
        JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellation)
    {
        try
        {
            using var client = CreateHttpClient();
            var response = await sendRequest(client);
            var body = await response.Content.ReadAsStringAsync(cancellation);

            if (!response.IsSuccessStatusCode)
                throw new Exception(
                    $"AI service returns error code {response.StatusCode}. Body: {body ?? string.Empty}");

            return JsonSerializer.Deserialize(body, jsonTypeInfo);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to call OpenAI service");
            Services.Notification?.Show(Lang.Warning, ex.Message, Severity.Warning, TimeSpan.FromSeconds(8));
            return default;
        }
    }
}

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    IgnoreReadOnlyFields = true,
    IgnoreReadOnlyProperties = true
)]
[JsonSerializable(typeof(OpenAIChatRequest))]
[JsonSerializable(typeof(OpenAIChatResponse))]
[JsonSerializable(typeof(OpenAIModelsResponse))]
[JsonSerializable(typeof(List<ModelData>))]
[JsonSerializable(typeof(ModelData))]
[JsonSerializable(typeof(List<ModelPermission>))]
[JsonSerializable(typeof(ModelPermission))]
public partial class OpenAIContent : JsonSerializerContext;