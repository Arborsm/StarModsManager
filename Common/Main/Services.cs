using System.Reflection;
using Avalonia.Controls;
using StarModsManager.Common.Config;
using StarModsManager.Common.Trans;

namespace StarModsManager.Common.Main;

public static class Services
{
    public static readonly string AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
    public static readonly string AppSavingPath = 
        CombineNCheckDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StarModsManager");
    private static readonly string TempDir = CombineNCheckDirectory(Path.GetTempPath(), "StarModsManager");
    public static readonly string BackupTempDir = CombineNCheckDirectory(TempDir, "Backup");
    public static readonly string OnlineModsDir = CombineNCheckDirectory(TempDir, "OnlineMods");
    public static readonly string LogDir = CombineNCheckDirectory(AppSavingPath, "Log");
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

    private static string CombineNCheckDirectory(params string[] directories)
    {
        var path = Path.Combine(directories);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }
}