using StarModsManager.Common.Config;
using StarModsManager.Common.Trans;

namespace StarModsManager.Api;

public static class Services
{
    public const string AppVersion = "1.0.0";
    public static readonly string AppSavingPath =
        CombineNCheckDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StarModsManager");
    public static readonly string ModLabelsPath = CombineNCheckDirectory(AppSavingPath, "ModLabels");
    public static readonly string TempDir = CombineNCheckDirectory(Path.GetTempPath(), "StarModsManager");
    public static readonly string BackupTempDir = CombineNCheckDirectory(TempDir, "Backup");
    public static readonly string OnlineModsDir = CombineNCheckDirectory(TempDir, "OnlineMods");
    public static readonly string LogDir = CombineNCheckDirectory(AppSavingPath, "Log");
    public static MainConfig MainConfig => new();
    public static TransConfig TransConfig => new();
    public static ProofreadConfig ProofreadConfig => new();
    public static Dictionary<string, TransApiConfig> TransApiConfigs { get; } =
        Translator.Instance.Apis.ToDictionary(t => t.Name, t => new TransApiConfig(t.Name));
    public static INotification Notification { get; set; } = null!;
    public static IProgress Progress { get; set; } = null!;

    private static string CombineNCheckDirectory(params string[] directories)
    {
        var path = Path.Combine(directories);
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        return path;
    }
}