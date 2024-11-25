using System.Reflection;
using StarModsManager.Api.NexusMods;
using StarModsManager.Config;
using StarModsManager.Trans;

namespace StarModsManager.Api;

public static class Services
{
    public const string AppName = "StarModsManager";
    public static readonly string AppVersion = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
    public static readonly string AppSavingPath = CombineNCheckDirectory(SystemSavingPath, AppName);
    public static readonly string DownloadPath = CombineNCheckDirectory(AppSavingPath, "Downloads");
    public static readonly string ModLabelsPath = CombineNCheckDirectory(AppSavingPath, "ModLabels");
    public static readonly string ModPicsPath = CombineNCheckDirectory(AppSavingPath, "Pics");
    public static readonly string TempDir = CombineNCheckDirectory(Path.GetTempPath(), AppName);
    public static readonly string BackupTempDir = CombineNCheckDirectory(TempDir, "Backup");
    public static readonly string OnlineModsDir = CombineNCheckDirectory(TempDir, "OnlineMods");
    public static readonly string LogDir = CombineNCheckDirectory(AppSavingPath, "Log");

    public static readonly string BackupPath =
        CombineNCheckDirectory(AppSavingPath, "Backup", DateTime.Now.ToString("yyyyMMdd"));

    static Services()
    {
        NexusManager.Initialize(MainConfig.NexusModsApiKey, $"StarModsManager/{AppVersion}");
    }

    public static MainConfig MainConfig { get; } = MainConfig.LoadOrCreate();
    public static TransConfig TransConfig { get; } = TransConfig.LoadOrCreate();
    public static ProofreadConfig ProofreadConfig { get; } = ProofreadConfig.LoadOrCreate();

    public static Dictionary<string, TransApiConfig> TransApiConfigs { get; } =
        Translator.Instance.Apis.ToDictionary(t => t.Name, t => TransApiConfig.LoadOrCreate(t.Name));

    public static INotification Notification { get; set; } = null!;
    public static IProgress Progress { get; set; } = null!;
    public static IDialog Dialog { get; set; } = null!;
    public static IPopUp PopUp { get; set; } = null!;
    public static ILifeCycle LifeCycle { get; set; } = null!;

    private static string SystemSavingPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    private static string CombineNCheckDirectory(params string[] directories)
    {
        var path = Path.Combine(directories);
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }
}