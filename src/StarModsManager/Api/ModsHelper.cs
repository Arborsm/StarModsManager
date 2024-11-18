using System.Text.Json;
using Serilog;
using StardewModdingAPI.Toolkit.Framework.GameScanning;
using StarModsManager.Assets;
using StarModsManager.Mods;

namespace StarModsManager.Api;

public class ModsHelper
{
    public static readonly ModsHelper Instance = new();
    public readonly List<LocalMod> I18LocalMods = [];
    public bool IsMismatchedTokens = false;
    public Dictionary<string, LocalMod> LocalModsMap = []; // Id/UniqueId -> mod
    public Dictionary<string, OnlineMod> OnlineModsMap = []; // UniqueId -> mod
    public readonly IEnumerable<DirectoryInfo> GameFolders = new GameScanner().Scan();

    private ModsHelper()
    {
    }

    public async Task FindModsAsync()
    {
        var path = Services.MainConfig.DirectoryPath;
        Log.Information("Searching Mods in Dir: {Path}", path);
        if (!Directory.Exists(path)) Console.WriteLine(Strings.ErrorFoldersMsg);
        OnlineModsMap = (await LoadCachedAsync()).ToDictionary(mod => mod.ModId, mod => mod);
        LocalModsMap = new Dictionary<string, LocalMod>();
        var manifestFiles = new List<string>();
        await FindManifestFilesAsync(path, manifestFiles);
        LocalModsMap = manifestFiles.AsParallel()
            .Select(manifestFilePath => new LocalMod(manifestFilePath))
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
                if (mod is not null) results.Add(mod);
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
            Console.WriteLine(Strings.ErrorOccurred);
            Console.WriteLine(ex.Message);
        }
    }
}