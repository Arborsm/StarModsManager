using OllamaSharp;
using OllamaSharp.Models.Chat;
using StarModsManager.Common.Config;
using Message = OllamaSharp.Models.Chat.Message;

namespace StarModsManager.Common.Trans;

internal class OllamaTrans : ITranslator
{
    public bool NeedApi => false;
    public string Name => "Ollama";

    public async Task<string> StreamCallWithMessageAsync(string text, string role, TransApiConfig config,
        CancellationToken cancellationToken)
    {
        var ollama = new OllamaApiClient(config.Url);
        var messages = new List<Message>
        {
            new(ChatRole.System, role),
            new(ChatRole.User, text)
        };
        var chatRequest = new ChatRequest
        {
            Messages = messages,
            Model = config.Model
        };
        var response = await ollama.ChatAsync(chatRequest, cancellationToken).StreamToEndAsync();
        return response?.Message.Content ?? string.Empty;
    }

    public async Task<List<string>> GetSupportModelsAsync(TransApiConfig config)
    {
        var ollama = new OllamaApiClient(config.Url);
        var models = await ollama.ListLocalModelsAsync();
        return models.Select(it => it.Name).ToList();
    }
}