using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using StarModsManager.Api.SMAPI;
using StarModsManager.Mods;
using StarModsManager.Trans;

namespace StarModsManager.Api;

public static class SMMHelper
{
    private const string WindowsUncRoot = @"\\";

    public static readonly Dictionary<string, string> LanguageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["中文"] = "zh",
        ["Français"] = "fr",
        ["Deutsch"] = "de",
        ["Magyar"] = "hu",
        ["Italiano"] = "it",
        ["日本語"] = "ja",
        ["한국어"] = "ko",
        ["Português"] = "pt",
        ["Русский"] = "ru",
        ["Español"] = "es",
        ["Türkçe"] = "tr"
    };

    private static SemaphoreSlim? _semaphore;

    private static readonly Dictionary<ModSiteKey, string> VendorModUrls = new()
    {
        [ModSiteKey.Chucklefish] = "https://community.playstarbound.com/resources/{0}",
        [ModSiteKey.CurseForge] = "https://www.curseforge.com/projects/{0}",
        [ModSiteKey.GitHub] = "https://github.com/{0}/releases",
        [ModSiteKey.ModDrop] = "https://www.moddrop.com/stardew-valley/mods/{0}",
        [ModSiteKey.Nexus] = "https://www.nexusmods.com/stardewvalley/mods/{0}"
    };

    private static readonly char[] PossiblePathSeparators =
        new[] { '/', '\\', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }.Distinct().ToArray();

    public static string SwitchLanguage(string input)
    {
        if (LanguageMap.TryGetValue(input, out var code)) return code;

        return LanguageMap.ContainsValue(input)
            ? LanguageMap.First(x => x.Value.Equals(input, StringComparison.OrdinalIgnoreCase)).Key
            : string.Empty;
    }

    public static void ForEach<TSource>(this IEnumerable<TSource> source,
        Action<TSource> action, CancellationToken cancellationToken = default)
    {
        foreach (var item in source)
        {
            if (cancellationToken.IsCancellationRequested) break;
            action(item);
        }
    }

    public static async Task ForEachAsync<T>(this IEnumerable<T> items, Func<T, Task> action, int maxConcurrent = 1)
    {
        _semaphore = new SemaphoreSlim(maxConcurrent);
        var tasks = new List<Task>();

        foreach (var item in items)
        {
            await _semaphore.WaitAsync();

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await action(item);
                }
                finally
                {
                    _semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
    }

    public static Dictionary<TKey, TValue> Sort<TKey, TValue>(
        this Dictionary<TKey, TValue> unsorted,
        Dictionary<TKey, TValue> reference) where TKey : notnull
    {
        var sortedTarget = new Dictionary<TKey, TValue>();

        foreach (var kvp in reference.Where(kvp => unsorted.ContainsKey(kvp.Key)))
            sortedTarget[kvp.Key] = unsorted[kvp.Key];

        return sortedTarget;
    }

    public static (Dictionary<string, string> defaultLang, Dictionary<string, string> targetLang) ReadMap(
        this LocalMod localMod,
        string? lang = null)
    {
        lang ??= Services.TransConfig.Language;
        var files = Directory.GetFiles(localMod.PathS + "\\i18n", "*.json");
        var defaultLangFile = files.First(x => x.Contains("default"));
        var targetLangFile = files.FirstOrDefault(x => x.Contains(lang));
        var defaultLang = TranslationContext.GetMap(File.ReadAllText(defaultLangFile));
        var targetLangTemp = targetLangFile != null
            ? TranslationContext.GetMap(File.ReadAllText(targetLangFile))
            : new Dictionary<string, string>();
        var targetLang = targetLangTemp.Sort(defaultLang);
        return (defaultLang, targetLang);
    }

    public static Dictionary<string, (string?, string?)> LoadLangMap(this LocalMod currentMod)
    {
        var (defaultLang, targetLang) = currentMod.ReadMap(Services.TransConfig.Language);
        return defaultLang.Keys.Union(targetLang.Keys)
            .ToDictionary(key => key, key => (defaultLang.GetValueOrDefault(key), targetLang.GetValueOrDefault(key)));
    }

    public static bool? IsMisMatch(this string? sourceText, string? targetText)
    {
        return sourceText?.IsMismatchedTokens(targetText);
    }

    public static Dictionary<string, string> GetUntranslatedMap(this LocalMod localMod)
    {
        var (defaultLang, targetLang) = localMod.ReadMap();
        return defaultLang.Where(x => !targetLang.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
    }

    public static string TrimEndNewLine(this string str)
    {
        return string.IsNullOrEmpty(str) ? str : str.TrimEnd('\r', '\n');
    }

    public static void CreateZipBackup(this string directoryPath, string zipFileName)
    {
        var backupPath = Services.BackupPath;
        var zipFilePath = Path.Combine(backupPath, $"{zipFileName}.zip");
        if (File.Exists(zipFilePath))
        {
            zipFilePath = Path.Combine(backupPath, $"{zipFileName}_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.zip");
        }

        using (var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
        {
            ZipDirectory(directoryPath, archive);
        }

        var isSame = Directory.GetFiles(backupPath)
            .Where(x => x.Contains(zipFileName) && x != zipFilePath)
            .Any(x => IsFileSame(x, zipFilePath));
        if (isSame) File.Delete(zipFilePath);
    }

    private static bool IsFileSame(string sourceFile, string backupFile)
    {
        try
        {
            var sourceInfo = new FileInfo(sourceFile);
            var backupInfo = new FileInfo(backupFile);
            if (sourceInfo.Length == backupInfo.Length) return true;
            using var md5 = MD5.Create();
            using var sourceStream = File.OpenRead(sourceFile);
            using var backupStream = File.OpenRead(backupFile);
            var sourceHash = Convert.ToHexString(md5.ComputeHash(sourceStream));
            md5.Initialize();
            var backupHash = Convert.ToHexString(md5.ComputeHash(backupStream));
            return string.Equals(sourceHash, backupHash, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void ZipDirectory(string directoryPath, ZipArchive archive, string entryRootPath = "")
    {
        foreach (var filePath in Directory.GetFiles(directoryPath))
        {
            var relativeFilePath = Path.Combine(entryRootPath, Path.GetFileName(filePath));
            archive.CreateEntryFromFile(filePath, relativeFilePath);
        }

        foreach (var directory in Directory.GetDirectories(directoryPath))
        {
            var relativeDirectoryPath = Path.Combine(entryRootPath, Path.GetFileName(directory));
            ZipDirectory(directory, archive, relativeDirectoryPath);
        }
    }

    public static string SanitizePath(this string path)
    {
        return Path.GetInvalidFileNameChars().Aggregate(path, (current, c) => current.Replace(c, '_'));
    }

    private static string GetJsonString(this string directoryPath, string fileName)
    {
        var i18NDir = Directory.GetDirectories(directoryPath, "i18n", SearchOption.AllDirectories).FirstOrDefault();
        if (i18NDir == null) return string.Empty;
        var filePath = Path.Combine(i18NDir, fileName);
        return File.Exists(filePath) ? File.ReadAllText(filePath, Encoding.UTF8) : string.Empty;
    }

    public static Dictionary<string, string> GetTargetLang(this string path, string fileName)
    {
        return TranslationContext.GetMap(path.GetJsonString(fileName));
    }

    public static Dictionary<string, string> GetTargetLang(this string path)
    {
        return GetTargetLang(path, Services.TransConfig.Language + ".json");
    }

    public static Dictionary<string, string> GetDefaultLang(this string path)
    {
        return TranslationContext.GetMap(path.GetJsonString("default.json"));
    }

    public static bool CheckModDependency(this IEnumerable<LocalMod> mods,
        ManifestDependency dependency)
    {
        return mods.Any(mod =>
            mod.Manifest.UniqueID == dependency.UniqueID &&
            mod.Manifest.Version.ComparePrecedenceTo(dependency.MinimumVersion) != -1);
    }

    public static bool CheckModDependency(this IEnumerable<LocalMod> mods,
        ManifestContentPackFor manifestContentPackFor)
    {
        return mods.Any(mod =>
            mod.Manifest.ContentPackFor != null &&
            mod.Manifest.ContentPackFor.UniqueID == manifestContentPackFor.UniqueID &&
            mod.Manifest.Version.ComparePrecedenceTo(manifestContentPackFor.MinimumVersion) != -1);
    }

    public static string? GetUpdateUrl(string updateKey)
    {
        var parsed = UpdateKey.Parse(updateKey);
        if (parsed.LooksValid == null || !parsed.LooksValid.Value) return null;

        return VendorModUrls.TryGetValue(parsed.Site, out var urlTemplate)
            ? string.Format(urlTemplate, parsed.ID)
            : null;
    }

    public static IEnumerable<string> GetDefaultInstallPaths()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            const string steamAppId = "413150";
            IDictionary<string, string> registryKeys = new Dictionary<string, string>
            {
                [@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App " + steamAppId] = "InstallLocation",
                [@"SOFTWARE\WOW6432Node\GOG.com\Games\1453375253"] = "PATH"
            };
            foreach (var pair in registryKeys)
            {
                var path = GetLocalMachineRegistryValue(pair.Key, pair.Value);
                if (!string.IsNullOrWhiteSpace(path))
                    yield return path;
            }

            var steamPath = GetCurrentUserRegistryValue(@"Software\Valve\Steam", "SteamPath");
            if (steamPath != null)
                yield return Path.Combine(steamPath.Replace('/', '\\'), @"steamapps\common\Stardew Valley");

            foreach (var programFiles in new[] { @"C:\Program Files", @"C:\Program Files (x86)" })
            {
                yield return $@"{programFiles}\GalaxyClient\Games\Stardew Valley";
                yield return $@"{programFiles}\GOG Galaxy\Games\Stardew Valley";
                yield return $@"{programFiles}\GOG Games\Stardew Valley";
                yield return $@"{programFiles}\Steam\steamapps\common\Stardew Valley";
            }

            for (var driveLetter = 'C'; driveLetter <= 'H'; driveLetter++)
                yield return $@"{driveLetter}:\Program Files\ModifiableWindowsApps\Stardew Valley";
        }

        yield return Environment.CurrentDirectory;
    }

    [SupportedOSPlatform("windows")]
    private static string? GetLocalMachineRegistryValue(string key, string name)
    {
        var localMachine = Environment.Is64BitOperatingSystem
            ? RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
            : Registry.LocalMachine;
        var openKey = localMachine.OpenSubKey(key);
        if (openKey == null)
            return null;
        using (openKey)
        {
            return (string?)openKey.GetValue(name);
        }
    }

    [SupportedOSPlatform("windows")]
    private static string? GetCurrentUserRegistryValue(string key, string name)
    {
        var currentUser = Environment.Is64BitOperatingSystem
            ? RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
            : Registry.CurrentUser;
        var openKey = currentUser.OpenSubKey(key);
        if (openKey == null)
            return null;
        using (openKey)
        {
            return (string?)openKey.GetValue(name);
        }
    }

    [Pure]
    [return: NotNullIfNotNull("path")]
    public static string? NormalizePath(string? path)
    {
        path = path?.Trim();
        if (string.IsNullOrEmpty(path))
            return path;

        // get a basic path format (e.g. /some/asset\\path/ => some\asset\path)
        var segments = GetSegments(path);
        var newPath = string.Join(Path.DirectorySeparatorChar.ToString(), segments);

        // keep root prefix
        var hasRoot = false;
        if (path.StartsWith(WindowsUncRoot))
        {
            newPath = WindowsUncRoot + newPath;
            hasRoot = true;
        }
        else if (PossiblePathSeparators.Contains(path[0]))
        {
            newPath = Path.DirectorySeparatorChar + newPath;
            hasRoot = true;
        }

        // keep trailing separator
        if ((!hasRoot || segments.Length != 0) && PossiblePathSeparators.Contains(path[^1]))
            newPath += Path.DirectorySeparatorChar;

        return newPath;
    }

    [Pure]
    private static string[] GetSegments(string? path, int? limit = null)
    {
        if (path == null) return [];

        return limit.HasValue
            ? path.Split(PossiblePathSeparators, limit.Value, StringSplitOptions.RemoveEmptyEntries)
            : path.Split(PossiblePathSeparators, StringSplitOptions.RemoveEmptyEntries);
    }

    private enum ModSiteKey
    {
        Unknown,
        Chucklefish,
        CurseForge,
        GitHub,
        ModDrop,
        Nexus
    }

    private class UpdateKey(ModSiteKey site, string? id)
    {
        public string? ID { get; } = id;

        public ModSiteKey Site { get; } = site;

        [MemberNotNullWhen(true, nameof(ID))]
        public bool? LooksValid { get; } = site != ModSiteKey.Unknown && !string.IsNullOrWhiteSpace(id);

        public static UpdateKey Parse(string? raw)
        {
            if (raw is null) return new UpdateKey(ModSiteKey.Unknown, null);
            var (rawSite, id) = SplitTwoParts(raw, ':');
            if (string.IsNullOrEmpty(id)) id = null;
            if (id != null) (id, _) = SplitTwoParts(id, '@', true);
            if (!Enum.TryParse(rawSite, true, out ModSiteKey site))
                return new UpdateKey(ModSiteKey.Unknown, id);
            return id == null ? new UpdateKey(site, null) : new UpdateKey(site, id);
        }

        private static (string, string?) SplitTwoParts(string str, char delimiter, bool keepDelimiter = false)
        {
            var splitIndex = str.IndexOf(delimiter);
            return splitIndex >= 0
                ? (str[..splitIndex].Trim(), str[(splitIndex + (keepDelimiter ? 0 : 1))..].Trim())
                : (str.Trim(), null);
        }
    }
}

public class KeyValuePairComparer<TKey, TValue>(
    IEqualityComparer<TKey>? keyComparer = null,
    IEqualityComparer<TValue>? valueComparer = null)
    : IEqualityComparer<KeyValuePair<TKey, TValue>>
{
    private readonly IEqualityComparer<TKey> _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
    private readonly IEqualityComparer<TValue> _valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;

    public bool Equals(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
    {
        return _keyComparer.Equals(x.Key, y.Key) && _valueComparer.Equals(x.Value, y.Value);
    }

    public int GetHashCode(KeyValuePair<TKey, TValue> obj)
    {
        var keyHashCode = obj.Key != null ? _keyComparer.GetHashCode(obj.Key) : 0;
        var valueHashCode = obj.Value != null ? _valueComparer.GetHashCode(obj.Value) : 0;

        return (keyHashCode * 397) ^ valueHashCode;
    }
}

public class ModLangIsMatchComparer : IComparer
{
    public int Compare(object? x, object? y)
    {
        return Compare(x as ModLang, y as ModLang);
    }

    private static int Compare(ModLang? x, ModLang? y)
    {
        return x?.IsMisMatch.GetValueOrDefault() == y?.IsMisMatch.GetValueOrDefault() ? 0 : 1;
    }
}