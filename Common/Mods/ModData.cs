﻿using System.Text.Json;
using StarModsManager.Assets.Lang;
using StarModsManager.Common.Main;

namespace StarModsManager.Common.Mods;

internal class ModData
{
    public bool IsMismatchedTokens = false;
    public static readonly ModData Instance = new();
    public readonly List<LocalMod> I18LocalMods = [];
    public readonly List<OnlineMod> OnlineMods = [];
    public Dictionary<string, LocalMod> LocalModsMap = []; // Id/UniqueId -> mod
    public Dictionary<string, OnlineMod> OnlineModsMap = []; // UniqueId -> mod
    
    public async Task FindModsAsync()
    {
        var path = Services.MainConfig.DirectoryPath;
        if (Directory.Exists(path))
        {
            OnlineModsMap = (await LoadCachedAsync()).ToDictionary(mod => mod.ModId, mod => mod);
            LocalModsMap = new Dictionary<string, LocalMod>();
            var manifestFiles = new List<string>();
            await FindManifestFilesAsync(path, manifestFiles);
            LocalModsMap = manifestFiles
                .AsParallel()
                .Select(manifestFile => new LocalMod(manifestFile))
                .GroupBy(mod => mod.UniqueID)
                .ToDictionary(
                    group => group.Key,
                    group => group.LastOrDefault()
                )!;

            InitProcessMods();
        }
        else
        {
            Console.WriteLine(Strings.ErrorFoldersMsg);
        }
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
                var mod = await LoadCachedAsync(fs).ConfigureAwait(false);
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
        return await JsonSerializer.DeserializeAsync<OnlineMod>(stream).ConfigureAwait(false);
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