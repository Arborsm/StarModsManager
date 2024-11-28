using StarModsManager.Api.AI;
using StarModsManager.Config;

namespace StarModsManager.Trans;

internal class OpenAITrans : ITranslator
{
    public bool NeedApi => true;
    public string Name => "OpenAI";

    public async Task<string> StreamCallWithMessageAsync(
        string text,
        string role,
        TransApiConfig config,
        CancellationToken cancellationToken)
    {
        var api = new OpenAIService(config.Model, config.Api, config.Url);
        var response = await api.ChatAsync(role, text, cancellationToken);
        return response?.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;
    }

    public async Task<List<string>> GetSupportModelsAsync(TransApiConfig config)
    {
        var api = new OpenAIService(config.Model, config.Api, config.Url);
        var models = await api.GetSupportModelsAsync(CancellationToken.None);
        return models?.Data.Select(it => it.Id).ToList() ?? [];
    }
}