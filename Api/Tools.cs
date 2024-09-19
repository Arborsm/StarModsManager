using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using NLog;
using StarModsManager.Common.Main;
using StarModsManager.Common.Mods;

namespace StarModsManager.Api;

internal static class Tools
{
    public static void ForEach<TSource>(
        this IEnumerable<TSource> source,
        Action<TSource> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }
    
    /// <summary>
    /// 根据参考字典的键顺序对字典进行排序。
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
    /// 读取本地化文件。
    /// </summary>
    /// <param name="localMod">本地化模块。</param>
    /// <param name="lang">语言代码。</param>
    /// <returns>默认语言和目标语言的字典。</returns>
    public static (Dictionary<string, string> defaultLang, Dictionary<string, string> targetLang) ReadMap(
        this LocalMod localMod, 
        string lang)
    {
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

    /// <summary>
    /// 将double值转换为整数字符串。
    /// </summary>
    /// <param name="value">要转换的double值。</param>
    /// <returns>整数字符串。</returns>
    public static string ToStringI(this double value)
    {
        return ((int)value).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 创建备份文件夹。
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
    /// 压缩文件夹。
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
        return GetTargetLang(path, Program.TransConfig.Language + ".json");
    }

    public static Dictionary<string, string> GetDefaultLang(this string path)
    {
        var en = path.GetJsonString("default.json");
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(en)!;
    }

    /// <summary>
    /// 扩展方法，用于测量异步任务的耗时并返回耗时和任务的结果。
    /// </summary>
    /// <typeparam name="T">任务结果的类型。</typeparam>
    /// <param name="task">需要测量的异步任务。</param>
    /// <param name="arg">可选输出文本</param>
    /// <param name="level">日志级别</param>
    /// <returns>任务的结果。</returns>
    public static async Task<T> WithDebugElapsedTime<T>(this Task<T> task, string? arg = default, LogLevel? level = default)
    {
        if (level == null) level = LogLevel.Debug;
        var stopwatch = Stopwatch.StartNew();
        var result = await task;
        stopwatch.Stop();
        StarDebug.Log($"{arg} completed in {stopwatch.ElapsedMilliseconds} ms", level);
        return result;
    }

    /// <summary>
    /// 扩展方法，用于测量无返回值异步任务的耗时。
    /// </summary>
    /// <param name="task">需要测量的异步任务。</param>
    /// <param name="arg">可选输出文本</param>
    /// /// <param name="level">日志级别</param>
    /// <returns>任务。</returns>
    public static async Task WithDebugElapsedTime(this Task task, string? arg = default, LogLevel? level = default)
    {
        if (level == null) level = LogLevel.Debug;
        var stopwatch = Stopwatch.StartNew();
        await task;
        stopwatch.Stop();
        StarDebug.Log($"{arg} completed in {stopwatch.ElapsedMilliseconds} ms", level);
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

internal enum ProcessResult
{
    Success,
    Fail,
    Cancel
}