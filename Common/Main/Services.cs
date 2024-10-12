using Avalonia.Controls;
using StarModsManager.Common.Config;
using StarModsManager.Common.Trans;

namespace StarModsManager.Common.Main;

public static class Services
{
    public static readonly string AppSavingPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StarModsManager");
    public static MainConfig MainConfig => ConfigManager<MainConfig>.GetInstance().Config;
    public static TransConfig TransConfig => ConfigManager<TransConfig>.GetInstance().Config;
    public static ProofreadConfig ProofreadConfig => ConfigManager<ProofreadConfig>.GetInstance().Config;
    public static Dictionary<string, TransApiConfig> TransApiConfigs { get; } = Translator.Instance.Apis
        .ToDictionary(t => t.Name, t => ConfigManager<TransApiConfig>.GetInstance(t.Name).Config);

    private static bool? _isInDesignMode;
    public static bool IsInDesignMode
    {
        get
        {
            _isInDesignMode ??= Design.IsDesignMode;
            return _isInDesignMode.Value;
        }
    }
}