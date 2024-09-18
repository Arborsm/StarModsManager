using OllamaSharp;
using OllamaSharp.Models.Chat;
using StarModsManager.Common.Config;
using Message = OllamaSharp.Models.Chat.Message;

namespace StarModsManager.Common.Trans;

internal class OllamaTrans : ITranslator
{
    public bool NeedApi => false;
    public string Name => "Ollama";

    public async Task<string> StreamCallWithMessage(string text, string role, TransConfig config,
        CancellationToken cancellationToken)
    {
        var ollama = new OllamaApiClient(config.TransApiConf.Url);
        var messages = new List<Message>
        {
            new(ChatRole.System, role),
            new(ChatRole.User, text)
        };
        var chatRequest = new ChatRequest
        {
            Messages = messages,
            Model = config.TransApiConf.Model
        };
        var response = await ollama.Chat(chatRequest, cancellationToken).StreamToEnd();
        return response?.Message.Content ?? string.Empty;
    }

    public async Task<List<string>> GetSupportModels(TransConfig config)
    {
        var ollama = new OllamaApiClient(config.TransApiConf.Url);
        var models = await ollama.ListLocalModels();
        return models.Select(it => it.Name).ToList();
    }
}