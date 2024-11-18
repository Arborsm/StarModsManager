using OpenAI;
using OpenAI.Chat;
using Message = OpenAI.Chat.Message;
using TransApiConfig = StarModsManager.Config.TransApiConfig;

namespace StarModsManager.Trans;

internal class OpenAITrans : ITranslator
{
    public bool NeedApi => true;
    public string Name => "OpenAI";

    public async Task<string> StreamCallWithMessageAsync(string text, string role, TransApiConfig config,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        var api = new OpenAIAuthentication(config.Api);
        var url = new OpenAIClientSettings(config.Url);
        var client = new OpenAIClient(api, url, httpClient);
        var messages = new List<Message>
        {
            new(Role.System, role),
            new(Role.User, text)
        };
        var chatRequest = new ChatRequest(messages, config.Model);

        var response = await client.ChatEndpoint.GetCompletionAsync(chatRequest, cancellationToken);

        return response.FirstChoice;
    }

    public async Task<List<string>> GetSupportModelsAsync(TransApiConfig config)
    {
        using var httpClient = new HttpClient();
        var api = new OpenAIAuthentication(config.Api);
        var url = new OpenAIClientSettings(config.Url);
        var client = new OpenAIClient(api, url, httpClient);
        var models = await client.ModelsEndpoint.GetModelsAsync();
        return models.Select(it => it.ToString()).ToList();
    }
}