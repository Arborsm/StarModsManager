using System.Text;
using System.Text.Json;
using Serilog;
using StarModsManager.Api;
using StarModsManager.Api.SMAPI;

namespace StarModsManager.Mods;

public sealed class LocalMod
{
    private bool? _isDependencyMissing;
    public string? MissingDependencies;

    private LocalMod(string path)
    {
        var manifestPath = Path.GetFullPath(path);
        var parentDirectory = Path.GetDirectoryName(manifestPath)!;
        var manifest = File.ReadAllText(path);
        List<string> i18N = [];
        if (Directory.Exists(Path.Combine(parentDirectory, "i18n")))
        {
            var i18NFiles = Directory.GetFiles(parentDirectory + @"\\i18n", "*.json");
            i18N.AddRange(i18NFiles.Select(Path.GetFileNameWithoutExtension)!);
        }

        Manifest = JsonSerializer.Deserialize(manifest, ManifestContent.Default.Manifest)!;
        Lang = i18N;
        PathS = parentDirectory;
        var updateKeys = Manifest.UpdateKeys;
        var numberPart = string.Empty;
        var modUrl = updateKeys.Select(SMMHelper.GetUpdateUrl).FirstOrDefault(p => p != null) ?? "";

        var picUrl = string.Empty;
        if (File.Exists(Path.Combine(Services.MainConfig.CachePath, numberPart + ".bmp")))
            picUrl = Path.Combine(Services.MainConfig.CachePath, numberPart + ".bmp");

        OnlineMod = ModsHelper.Instance.OnlineModsMap.TryGetValue(numberPart, out var onlineMod)
            ? onlineMod
            : new OnlineMod(modUrl, Manifest.Name, picUrl);

        if (Lang.Count < 1) return;
        LazyIsMisMatch = new(() =>
        {
            var t = this.ReadMap();
            return t.defaultLang.IsMismatchedTokens(t.targetLang);
        });
    }

    public bool IsDependencyMissing => _isDependencyMissing ??= CheckDependencies();
    public Manifest Manifest { get; }
    public string InfoPicturePath => Path.Combine(Services.ModPicsPath, $"{Manifest.UniqueID}.bmp");
    public OnlineMod OnlineMod { get; }
    public string PathS { get; }
    public List<string> Lang { get; }
    public Lazy<bool?> LazyIsMisMatch { get; } = new(() => false);

    public static LocalMod? Create(string path)
    {
        try
        {
            return new LocalMod(path);
        }
        catch (Exception e)
        {
            var name = Path.GetDirectoryName(SMMHelper.FindCommonPath(path, Services.MainConfig.DirectoryPath));
            Services.Notification?.Show(Assets.Lang.Warning,
                string.Format(Assets.Lang.CannotLoadMod, name), Severity.Warning);
            Log.Error(e, "Failed to create LocalMod instance.");
            return null;
        }
    }

    private bool CheckDependencies()
    {
        var isDependencyMissing = false;
        var sb = new StringBuilder("Missing: ");
        var mods = ModsHelper.Instance.LocalModsMap.Values;
        if (Manifest.ContentPackFor != null && !mods.CheckModDependency(Manifest.ContentPackFor))
        {
            Log.Warning("{Name} is a content pack for {UniqueID}, but the pack is not installed.",
                Manifest.Name, Manifest.ContentPackFor.UniqueID);
            sb.AppendLine(Manifest.ContentPackFor.UniqueID.Split('.').Last());
            isDependencyMissing = true;
        }

        foreach (var requiredDependency in Manifest.Dependencies.Where(it => it.IsRequired))
        {
            if (mods.CheckModDependency(requiredDependency)) continue;
            Log.Warning("{Name} requires {UniqueID}, but the mod is not installed.",
                Manifest.Name, requiredDependency.UniqueID);
            sb.AppendLine(requiredDependency.UniqueID.Split('.').Last());
            isDependencyMissing = true;
        }

        if (isDependencyMissing) MissingDependencies = sb.Remove(sb.Length - 2, 2).ToString();
        return isDependencyMissing;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not LocalMod other) return false;
        return other.Manifest == Manifest;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Manifest);
    }
}