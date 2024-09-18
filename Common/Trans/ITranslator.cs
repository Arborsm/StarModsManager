using StarModsManager.Common.Config;

namespace StarModsManager.Common.Trans;

public interface ITranslator
{
    bool NeedApi { get; }
    string Name { get; }

    Task<string> StreamCallWithMessage(string text, string role, TransConfig config,
        CancellationToken cancellationToken);

    Task<List<string>> GetSupportModels(TransConfig config);
}