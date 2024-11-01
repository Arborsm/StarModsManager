using System.Collections;
using System.IO.Compression;
using System.Text;
using StardewModdingAPI.Toolkit;
using StarModsManager.Common.Mods;
using StarModsManager.Common.Trans;

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
        var targetLangTemp = targetLangFile is not null 
            ? TranslationContext.GetMap(File.ReadAllText(targetLangFile)) : new Dictionary<string, string>();
        var targetLang = targetLangTemp.Sort(defaultLang);
        return (defaultLang, targetLang);
    }

    public static Dictionary<string, string> GetUntranslatedMap(this LocalMod localMod)
    {
        var (defaultLang, targetLang) = localMod.ReadMap();
        return defaultLang.Where(x => !targetLang.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
    }
    
    public static void CreateZipBackup(this string directoryPath, string zipFileName)
    {
        var backupPath = Path.Combine(Environment.CurrentDirectory, "Backup");
        if (!Directory.Exists(backupPath)) Directory.CreateDirectory(backupPath);
        var zipFilePath = Path.Combine(backupPath, zipFileName);
        using var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create);
        ZipDirectory(directoryPath, archive);
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

    public static string SanitizePath(this string path) => Path.GetInvalidFileNameChars().Aggregate(path, (current, c) => current.Replace(c, '_'));

    private static string GetJsonString(this string directoryPath, string fileName)
    {
        var i18NDir = Directory.GetDirectories(directoryPath, "i18n", SearchOption.AllDirectories).FirstOrDefault();
        if (i18NDir == null) return string.Empty;
        var filePath = Path.Combine(i18NDir, fileName);
        return File.Exists(filePath) ? File.ReadAllText(filePath, Encoding.UTF8) : string.Empty;
    }

    public static Dictionary<string, string> GetTargetLang(this string path, string fileName) => TranslationContext.GetMap(path.GetJsonString(fileName));

    public static Dictionary<string, string> GetTargetLang(this string path) => GetTargetLang(path, Services.TransConfig.Language + ".json");

    public static Dictionary<string, string> GetDefaultLang(this string path) => TranslationContext.GetMap(path.GetJsonString("default.json"));
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
        var keyHashCode = obj.Key is not null ? _keyComparer.GetHashCode(obj.Key) : 0;
        var valueHashCode = obj.Value is not null ? _valueComparer.GetHashCode(obj.Value) : 0;

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
        return x?.IsMatch.GetValueOrDefault() == y?.IsMatch.GetValueOrDefault() ? 0 : 1;
    }
}