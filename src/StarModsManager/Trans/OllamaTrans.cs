using StarModsManager.Api.AI;
using StarModsManager.Config;

namespace StarModsManager.Trans;

internal class OllamaTrans : ITranslator
{
    public bool NeedApi => false;
    public string Name => "Ollama";

    public async Task<string> StreamCallWithMessageAsync(string text, string role, TransApiConfig config,
        CancellationToken cancellationToken)
    {
        var api = new OllamaService(config.Model, config.Url);
        var response = await api.ChatAsync(role, text, cancellationToken);
        return response?.Message.Content ?? string.Empty;
    }

    public async Task<List<string>> GetSupportModelsAsync(TransApiConfig config)
    {
        var api = new OllamaService(config.Model, config.Url);
        var models = await api.GetSupportModelsAsync(CancellationToken.None);
        return models?.Models.Select(it => it.Name).ToList() ?? [];
    }
}