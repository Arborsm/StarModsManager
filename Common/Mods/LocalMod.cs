using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Serialization.Models;
using StarModsManager.Api;
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

        var mod = new JsonHelper().Deserialize<Manifest>(manifest)!;
        Name = mod.Name;
        Description = mod.Description;
        Version = mod.Version.ToString();
        UniqueID = mod.UniqueID;
        Lang = i18n;
        PathS = parentDirectory;
        var UpdateKeys = mod.UpdateKeys;
        var numberPart = string.Empty;
        var modUrl = UpdateKeys.Select(SMMHelper.Toolkit.GetUpdateUrl).FirstOrDefault(p => p != null) ?? "";

        var picUrl = string.Empty;
        if (File.Exists(Path.Combine(Services.MainConfig.CachePath, numberPart + ".bmp")))
            picUrl = Path.Combine(Services.MainConfig.CachePath, numberPart + ".bmp");

        OnlineMod = ModData.Instance.OnlineModsMap.TryGetValue(numberPart, out var onlineMod)
            ? onlineMod : new OnlineMod(modUrl, Name, picUrl);
    }

    public string InfoPicturePath => Path.Combine(PathS, "Info.bmp");

    public OnlineMod OnlineMod { get; }
    public string Name { get; }
    public string Description { get; }
    public string Version { get; }
    public string UniqueID { get; }
    public string PathS { get; }
    public List<string> Lang { get; }
    
    public override bool Equals(object? obj)
    {
        if (obj is not LocalMod other) return false;
        return other.Name == Name && other.Description == Description && other.Version == Version && other.UniqueID == UniqueID;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Description, Version, UniqueID);
    }
}