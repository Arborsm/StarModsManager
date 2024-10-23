using StarModsManager.Common.Config;

namespace StarModsManager.Common.Trans;

public interface ITranslator
{
    bool NeedApi { get; }
    string Name { get; }

    Task<string> StreamCallWithMessageAsync(string text, string role, TransApiConfig config,
        CancellationToken cancellationToken);

    Task<List<string>> GetSupportModelsAsync(TransApiConfig config);
}