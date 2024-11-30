using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using StarModsManager.Assets;
using StarModsManager.Mods;

namespace StarModsManager.Api;

public class ModsHelper : IDisposable
{
    public static readonly ModsHelper Instance = new();

    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private static readonly string NotGetPicTimesCachePath =
        Path.Combine(Services.AppSavingPath, "NotGetPicTimes.json");

    private static readonly string NotGetVersionTimesCachePath =
        Path.Combine(Services.AppSavingPath, "NotGetVersionTimes.json");

    private static readonly Dictionary<string, int> NotGetPicTimesMap; // ModId -> times
    private static readonly Dictionary<string, int> NotGetVersionTimesMap; // ModId -> times
    public readonly IEnumerable<string> GameFolders;
    public readonly List<LocalMod> I18LocalMods = [];
    public bool IsMismatchedTokens = false;
    public Dictionary<string, LocalMod> LocalModsMap = []; // Id/UniqueId -> mod
    public Dictionary<string, OnlineMod> OnlineModsMap = []; // UniqueId -> mod


    static ModsHelper()
    {
        if (!File.Exists(NotGetPicTimesCachePath)) File.WriteAllText(NotGetPicTimesCachePath, "{}");
        if (!File.Exists(NotGetVersionTimesCachePath)) File.WriteAllText(NotGetVersionTimesCachePath, "{}");
        NotGetPicTimesMap = JsonSerializer.Deserialize(File.ReadAllText(NotGetPicTimesCachePath),
            StringIntMapContent.Default.DictionaryStringInt32) ?? [];
        NotGetVersionTimesMap = JsonSerializer.Deserialize(File.ReadAllText(NotGetVersionTimesCachePath),
            StringIntMapContent.Default.DictionaryStringInt32) ?? [];
    }

    private ModsHelper()
    {
        GameFolders = SMMHelper.GetDefaultInstallPaths()
            .Select(SMMHelper.NormalizePath).Where(Directory.Exists).Cast<string>();
    }

    public void Dispose()
    {
        Semaphore?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task FindModsAsync()
    {
        var path = Services.MainConfig.DirectoryPath;
        Log.Information("Searching Mods in Dir: {Path}", path);
        if (!Directory.Exists(path)) Console.WriteLine(Lang.ErrorFoldersMsg);
        OnlineModsMap = (await LoadCachedAsync()).ToDictionary(mod => mod.ModId, mod => mod);
        LocalModsMap = new Dictionary<string, LocalMod>();
        var manifestFiles = new List<string>();
        await FindManifestFilesAsync(path, manifestFiles);
        LocalModsMap = manifestFiles.AsParallel()
            .Select(LocalMod.Create)
            .Where(mod => mod != null)
            .Cast<LocalMod>()
            .GroupBy(mod => mod.Manifest.UniqueID)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.LastOrDefault())!;
        InitProcessMods();
    }

    private static async Task<IEnumerable<OnlineMod>> LoadCachedAsync()
    {
        var cachePath = Services.OnlineModsDir;
        if (!Directory.Exists(cachePath)) Directory.CreateDirectory(cachePath);

        var results = new List<OnlineMod>();

        foreach (var file in Directory.EnumerateFiles(cachePath))
            try
            {
                if (!string.IsNullOrWhiteSpace(new DirectoryInfo(file).Extension) && file.EndsWith("bmp")) continue;

                await using var fs = File.OpenRead(file);
                var mod = await LoadCachedAsync(fs);
                if (mod != null) results.Add(mod);
            }
            catch (Exception)
            {
                File.Delete(file);
            }

        return results;
    }

    private static async Task<OnlineMod?> LoadCachedAsync(Stream stream)
    {
        return await JsonSerializer.DeserializeAsync(stream, OnlineModContent.Default.OnlineMod);
    }

    private void InitProcessMods()
    {
        I18LocalMods.Clear();
        var i18LocalMods = LocalModsMap.Values
            .AsParallel()
            .Where(mod => mod.Lang.Count > 0 && Directory.Exists(mod.PathS));
        I18LocalMods.AddRange(i18LocalMods);
    }

    private static async Task FindManifestFilesAsync(string path, List<string> manifestFiles)
    {
        try
        {
            var files = await Task.Run(() => Directory.GetFiles(path));
            manifestFiles.AddRange(files.Where(file =>
                Path.GetFileName(file).Equals("manifest.json", StringComparison.OrdinalIgnoreCase)));

            var directories = await Task.Run(() => Directory.GetDirectories(path));

            foreach (var directory in directories) await FindManifestFilesAsync(directory, manifestFiles);
        }
        catch (Exception ex)
        {
            Log.Error(ex, Lang.ErrorOccurred);
        }
    }

    public static int GetModNotGetPicTimes(string modId)
    {
        return NotGetPicTimesMap.GetValueOrDefault(modId, 0);
    }

    public static async Task AddModNotGetPicTimes(string modId)
    {
        NotGetPicTimesMap[modId] = GetModNotGetPicTimes(modId) + 1;
        await SaveMap(NotGetPicTimesMap, NotGetPicTimesCachePath);
    }

    public static int GetModNotGetVersionTimes(string modId)
    {
        return NotGetVersionTimesMap.GetValueOrDefault(modId, 0);
    }

    public static async Task AddModNotGetVersionTimes(string modId)
    {
        NotGetVersionTimesMap[modId] = GetModNotGetVersionTimes(modId) + 1;
        await SaveMap(NotGetVersionTimesMap, NotGetVersionTimesCachePath);
    }

    private static async Task SaveMap(Dictionary<string, int> map, string dir)
    {
        var json = JsonSerializer.Serialize(map, StringIntMapContent.Default.DictionaryStringInt32);
        if (await Semaphore.WaitAsync(TimeSpan.FromSeconds(5)))
            try
            {
                await File.WriteAllTextAsync(dir, json);
            }
            finally
            {
                Semaphore.Release();
            }
        else
            throw new TimeoutException("Unable to acquire lock to write file");
    }

    public static async Task RefreshMod(string modId)
    {
        NotGetPicTimesMap.Remove(modId);
        NotGetVersionTimesMap.Remove(modId);
        await SaveMap(NotGetPicTimesMap, NotGetPicTimesCachePath);
        await SaveMap(NotGetVersionTimesMap, NotGetVersionTimesCachePath);
    }

    public static void Reset()
    {
        Instance.LocalModsMap.Clear();
        Instance.OnlineModsMap.Clear();
        Instance.I18LocalMods.Clear();
    }
}

[JsonSerializable(typeof(Dictionary<string, int>))]
public partial class StringIntMapContent : JsonSerializerContext;