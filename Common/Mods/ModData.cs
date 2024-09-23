using System.Text.Json;
using StarModsManager.Assets.Lang;

namespace StarModsManager.Common.Mods;

internal class ModData
{
    public static readonly ModData Instance = new();
    public LocalMod[] I18LocalMods = null!;
    public bool IsMismatchedTokens = false;
    public Dictionary<string, LocalMod> LocalModsMap = null!; // Id/UniqueId -> mod
    public OnlineMod[] OnlineMods = null!;
    public Dictionary<string, OnlineMod> OnlineModsMap = null!; // UniqueId -> mod

    public async Task FindModsAsync(string path)
    {
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

    public static async Task<IEnumerable<OnlineMod>> LoadCachedAsync()
    {
        if (!Directory.Exists("./Cache")) Directory.CreateDirectory("./Cache");

        var results = new List<OnlineMod>();

        foreach (var file in Directory.EnumerateFiles("./Cache"))
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

    public static async Task<OnlineMod?> LoadCachedAsync(Stream stream)
    {
        return await JsonSerializer.DeserializeAsync<OnlineMod>(stream).ConfigureAwait(false);
    }

    public void InitProcessMods()
    {
        I18LocalMods = LocalModsMap.Values
            .AsParallel()
            .Where(mod => mod.Lang.Count > 0 && Directory.Exists(mod.PathS))
            .ToArray();
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