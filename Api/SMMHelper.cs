using System.IO.Compression;
using System.Text;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using Newtonsoft.Json;
using StardewModdingAPI.Toolkit;
using StarModsManager.Api.lib;
using StarModsManager.Common.Main;
using StarModsManager.Common.Mods;

namespace StarModsManager.Api;

internal static class SMMHelper
{
    public static readonly ModToolkit Toolkit = new();

    public static void Notification(string message, string title = "Info", InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        WeakReferenceMessenger.Default.Send(new NotificationMessage
        {
            Title = title,
            Message = message,
            Severity = severity
        });
    }
    
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
        if (LanguageMap.TryGetValue(input, out var code))
        {
            return code;
        }

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

    /// <summary>
    ///     根据参考字典的键顺序对字典进行排序。
    /// </summary>
    /// <typeparam name="TKey">字典键的类型。</typeparam>
    /// <typeparam name="TValue">字典值的类型。</typeparam>
    /// <param name="unsorted">要排序的字典。</param>
    /// <param name="reference">参考字典。</param>
    /// <returns>排序后的字典。</returns>
    public static Dictionary<TKey, TValue> Sort<TKey, TValue>(
        this Dictionary<TKey, TValue> unsorted,
        Dictionary<TKey, TValue> reference) where TKey : notnull
    {
        var sortedTarget = new Dictionary<TKey, TValue>();

        foreach (var kvp in reference.Where(kvp => unsorted.ContainsKey(kvp.Key)))
            sortedTarget[kvp.Key] = unsorted[kvp.Key];

        return sortedTarget;
    }

    /// <summary>
    ///     读取本地化文件。
    /// </summary>
    /// <param name="localMod">本地化模块。</param>
    /// <param name="lang">语言代码。</param>
    /// <returns>默认语言和目标语言的字典。</returns>
    public static (Dictionary<string, string> defaultLang, Dictionary<string, string> targetLang) ReadMap(
        this LocalMod localMod,
        string? lang = null)
    {
        lang ??= Services.TransConfig.Language;
        var files = Directory.GetFiles(localMod.PathS + "\\i18n", "*.json");
        var defaultLangFile = files.First(x => x.Contains("default"));
        var targetLangFile = files.FirstOrDefault(x => x.Contains(lang));
        var defaultLang = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(defaultLangFile))!;
        var targetLangTemp = targetLangFile is not null
            ? JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(targetLangFile))!
            : new Dictionary<string, string>();
        var targetLang = targetLangTemp.Sort(defaultLang);
        return (defaultLang, targetLang);
    }

    public static Dictionary<string, string> GetUntranslatedMap(this LocalMod localMod)
    {
        var (defaultLang, targetLang) = localMod.ReadMap();
        return defaultLang.Where(x => !targetLang.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
    }

    /// <summary>
    ///     创建备份文件夹。
    /// </summary>
    /// <param name="directoryPath">备份文件夹路径。</param>
    /// <param name="zipFileName">备份文件名。</param>
    public static void CreateZipBackup(this string directoryPath, string zipFileName)
    {
        var backupPath = Path.Combine(Environment.CurrentDirectory, "Backup");
        if (!Directory.Exists(backupPath)) Directory.CreateDirectory(backupPath);
        var zipFilePath = Path.Combine(backupPath, zipFileName);
        using var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create);
        ZipDirectory(directoryPath, archive);
    }

    /// <summary>
    ///     压缩文件夹。
    /// </summary>
    /// <param name="directoryPath">文件夹路径。</param>
    /// <param name="archive">压缩文件夹。</param>
    /// <param name="entryRootPath">压缩文件夹的根路径。</param>
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

    public static string GetJsonString(this string directoryPath, string fileName)
    {
        var i18NDir = Directory.GetDirectories(directoryPath, "i18n", SearchOption.AllDirectories).FirstOrDefault();
        if (i18NDir == null) return string.Empty;
        var filePath = Path.Combine(i18NDir, fileName);
        return File.Exists(filePath) ? File.ReadAllText(filePath, Encoding.UTF8) : string.Empty;
    }

    public static Dictionary<string, string> GetTargetLang(this string path, string fileName)
    {
        var target = path.GetJsonString(fileName);
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(target) ??
               new Dictionary<string, string>();
    }

    public static Dictionary<string, string> GetTargetLang(this string path)
    {
        return GetTargetLang(path, Services.TransConfig.Language + ".json");
    }

    public static Dictionary<string, string> GetDefaultLang(this string path)
    {
        var en = path.GetJsonString("default.json");
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(en)!;
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
        var keyHashCode = obj.Key is not null ? _keyComparer.GetHashCode(obj.Key) : 0;
        var valueHashCode = obj.Value is not null ? _valueComparer.GetHashCode(obj.Value) : 0;

        return (keyHashCode * 397) ^ valueHashCode;
    }
}