using System.Collections;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using StardewModdingAPI;
using StardewModdingAPI.Toolkit;
using StarModsManager.Mods;
using StarModsManager.Trans;

namespace StarModsManager.Api;

public static class SMMHelper
{
    public static readonly ModToolkit Toolkit = new();

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
            .Where(x => x.Contains(zipFileName))
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

    public static bool CheckModDependency(this IEnumerable<LocalMod> mods, IManifestDependency dependency)
    {
        return mods.Any(mod =>
            mod.Manifest.UniqueID == dependency.UniqueID &&
            !mod.Manifest.Version.IsOlderThan(dependency.MinimumVersion));
    }

    public static bool CheckModDependency(this IEnumerable<LocalMod> mods,
        IManifestContentPackFor manifestContentPackFor)
    {
        return mods.Any(mod =>
            mod.Manifest.ContentPackFor != null &&
            mod.Manifest.ContentPackFor.UniqueID == manifestContentPackFor.UniqueID &&
            !mod.Manifest.Version.IsOlderThan(manifestContentPackFor.MinimumVersion));
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