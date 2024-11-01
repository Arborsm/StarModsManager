using System.Text.Json;
using StarModsManager.Api;
using StarModsManager.Api.SMAPI;

namespace StarModsManager.Common.Mods;

public sealed class LocalMod
{
    private bool _isHidden;

    public LocalMod() : 
        this(@"E:\SteamLibrary\steamapps\common\Stardew Valley\mods\Romanceable Rasmodius Redux Revamp\Romanceable Rasmodius Redux Revamp\[CP] RRRR\manifest.json")
    {
    }

    public LocalMod(string path)
    {
        var manifestPath = Path.GetFullPath(path);
        var parentDirectory = Path.GetDirectoryName(manifestPath)!;
        var manifest = File.ReadAllText(path);
        List<string> i18N = [];
        try
        {
            var i18NFiles = Directory.GetFiles(parentDirectory + @"\\i18n", "*.json");
            i18N.AddRange(i18NFiles.Select(Path.GetFileNameWithoutExtension)!);
        }
        catch (Exception)
        {
            // ignored
        }

        var mod = JsonSerializer.Deserialize(manifest, ManifestContent.Default.Manifest)!;
        Name = mod.Name;
        Description = mod.Description;
        Version = mod.Version.ToString();
        UniqueID = mod.UniqueID;
        Lang = i18N;
        PathS = parentDirectory;
        _isHidden = File.Exists(Path.Combine(parentDirectory, ".Hidden"));
        var updateKeys = mod.UpdateKeys;
        var numberPart = string.Empty;
        var modUrl = updateKeys.Select(SMMHelper.Toolkit.GetUpdateUrl).FirstOrDefault(p => p != null) ?? "";

        var picUrl = string.Empty;
        if (File.Exists(Path.Combine(Services.MainConfig.CachePath, numberPart + ".bmp")))
            picUrl = Path.Combine(Services.MainConfig.CachePath, numberPart + ".bmp");

        OnlineMod = ModsHelper.Instance.OnlineModsMap.TryGetValue(numberPart, out var onlineMod)
            ? onlineMod
            : new OnlineMod(modUrl, Name, picUrl);
    }

    public string InfoPicturePath => Path.Combine(PathS, "Info.bmp");

    public OnlineMod OnlineMod { get; }
    public string Name { get; }
    public string Description { get; }
    public string Version { get; }
    public string UniqueID { get; }
    public string PathS { get; }
    public List<string> Lang { get; }

    public bool IsHidden
    {
        get => _isHidden;
        set
        {
            _isHidden = value;
            if (value)
                File.Create(Path.Combine(PathS, ".Hidden")).Close();
            else
                File.Delete(Path.Combine(PathS, ".Hidden"));
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is not LocalMod other) return false;
        return other.Name == Name && other.Description == Description && other.Version == Version &&
               other.UniqueID == UniqueID;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Description, Version, UniqueID);
    }
}