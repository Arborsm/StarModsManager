using System.IO.Compression;
using System.Text.Json;
using StarModsManager.Api.Lang;
using StarModsManager.Common.Mods;

namespace StarModsManager.Api;

public class ModsHelper
{
    public static readonly ModsHelper Instance = new();
    public readonly List<LocalMod> I18LocalMods = [];
    public bool IsMismatchedTokens = false;
    public Dictionary<string, LocalMod> LocalModsMap = []; // Id/UniqueId -> mod
    public Dictionary<string, OnlineMod> OnlineModsMap = []; // UniqueId -> mod

    public static void Install(string fileDir)
    {
        var targetDirectory = Services.MainConfig.DirectoryPath;
        try
        {
            using var archive = ZipFile.OpenRead(fileDir);
            var manifestEntry = archive.Entries.FirstOrDefault(e =>
                e.Name.Equals("manifest.json", StringComparison.OrdinalIgnoreCase));

            if (manifestEntry == null)
            {
                Services.Notification.Show("未找到manifest.json文件", "非Mod压缩包", Severity.Warning);
                return;
            }

            var topLevelDir = Path.GetDirectoryName(manifestEntry.FullName);
            if (string.IsNullOrEmpty(topLevelDir)) topLevelDir = Path.GetFileNameWithoutExtension(fileDir);

            var tempDir = Path.Combine(Services.TempDir, "ToInstallMod");
            Directory.CreateDirectory(tempDir);

            try
            {
                archive.ExtractToDirectory(tempDir, true);

                var sourceDir = Path.Combine(tempDir, topLevelDir);
                var destDir = Path.Combine(targetDirectory, topLevelDir);
                if (Directory.Exists(destDir)) Directory.Delete(destDir, true);

                MoveDirectory(Directory.Exists(sourceDir) ? sourceDir : tempDir, destDir);

                SMMDebug.Info($"Mod installed successfully: {topLevelDir}");
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }
        catch (InvalidDataException)
        {
            SMMDebug.Error("Error: Invalid zip file");
        }
        catch (Exception e)
        {
            SMMDebug.Error($"Error occurred during mod installation: {e.Message}");
        }
    }

    private static void MoveDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Move(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            MoveDirectory(dir, destSubDir);
        }
    }

    public async Task FindModsAsync()
    {
        var path = Services.MainConfig.DirectoryPath;
        SMMDebug.Info($"Searching Mods in Dir: {path}");
        if (!Directory.Exists(path)) Console.WriteLine(Strings.ErrorFoldersMsg);
        OnlineModsMap = (await LoadCachedAsync()).ToDictionary(mod => mod.ModId, mod => mod);
        LocalModsMap = new Dictionary<string, LocalMod>();
        var manifestFiles = new List<string>();
        await FindManifestFilesAsync(path, manifestFiles);
        LocalModsMap = manifestFiles.AsParallel()
            .Select(manifestFilePath => new LocalMod(manifestFilePath))
            .GroupBy(mod => mod.UniqueID)
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