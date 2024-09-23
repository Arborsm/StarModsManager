using Newtonsoft.Json;
using StarModsManager.Common.Main;

// ReSharper disable InconsistentNaming

namespace StarModsManager.Common.Mods;

public sealed class LocalMod
{
    public LocalMod() : this("E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\mods\\Romanceable Rasmodius Redux Revamp\\Romanceable Rasmodius Redux Revamp\\[CP] RRRR\\manifest.json") {}
    
    public LocalMod(string path)
    {
        var manifestPath = Path.GetFullPath(path);
        var parentDirectory = Path.GetDirectoryName(manifestPath)!;
        var manifest = File.ReadAllText(path);
        List<string> i18n = [];
        try
        {
            var i18nFiles = Directory.GetFiles(parentDirectory + @"\\i18n", "*.json");
            i18n.AddRange(i18nFiles.Select(Path.GetFileNameWithoutExtension)!);
        }
        catch (Exception)
        {
            // ignored
        }

        dynamic modData = JsonConvert.DeserializeObject(manifest)!;
        Name = modData.Name;
        Description = modData.Description;
        Version = modData.Version;
        UniqueID = modData.UniqueID;
        Lang = i18n;
        PathS = parentDirectory;
        var UpdateKeys = modData.UpdateKeys;
        string? modUrl = null;
        var numberPart = string.Empty;
        try
        {
            if (UpdateKeys is not null)
            {
                string updateKey = UpdateKeys[0];
                var parts = updateKey.Split(':');
                if (parts.Length > 1 && int.TryParse(parts[1], out var id)) numberPart = id.ToString();
                modUrl = "https://www.nexusmods.com/stardewvalley/mods/" + numberPart;
            }
        }
        catch (Exception)
        {
            // ignored
        }

        var picUrl = string.Empty;
        if (File.Exists(Path.Combine(Services.MainConfig.CachePath, numberPart + ".bmp")))
            picUrl = Path.Combine(Services.MainConfig.CachePath, numberPart + ".bmp");

        OnlineMod = ModData.Instance.OnlineModsMap.TryGetValue(numberPart, out var onlineMod)
            ? onlineMod
            : new OnlineMod(modUrl, Name, picUrl);
    }

    public string InfoPicturePath => Path.Combine(PathS, "Info.bmp");

    public OnlineMod OnlineMod { get; set; }
    public string Name { get; }
    public string Description { get; }
    public string Version { get; }
    public string UniqueID { get; }
    public string PathS { get; }
    public List<string> Lang { get; }
}