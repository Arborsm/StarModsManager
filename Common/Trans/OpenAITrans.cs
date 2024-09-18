using System.Net.Http;
using OpenAI;
using OpenAI.Chat;
using StarModsManager.Common.Config;
using Message = OpenAI.Chat.Message;

namespace StarModsManager.Common.Trans;

internal class OpenAITrans : ITranslator
{
    public bool NeedApi => true;
    public string Name => "OpenAI";

    public async Task<string> StreamCallWithMessage(string text, string role, TransConfig config,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        var api = new OpenAIAuthentication(config.TransApiConf.Api);
        var url = new OpenAIClientSettings(config.TransApiConf.Url);
        var client = new OpenAIClient(api, url, httpClient);
        var messages = new List<Message>
        {
            new(Role.System, role),
            new(Role.User, text)
        };
        var chatRequest = new ChatRequest(messages, config.TransApiConf.Model);

        var response = await client.ChatEndpoint.GetCompletionAsync(chatRequest, cancellationToken);

        return response.FirstChoice;
    }

    public async Task<List<string>> GetSupportModels(TransConfig config)
    {
        using var httpClient = new HttpClient();
        var api = new OpenAIAuthentication(config.TransApiConf.Api);
        var url = new OpenAIClientSettings(config.TransApiConf.Url);
        var client = new OpenAIClient(api, url, httpClient);
        var models = await client.ModelsEndpoint.GetModelsAsync();
        return models.Select(it => it.ToString()).ToList();
    }
}