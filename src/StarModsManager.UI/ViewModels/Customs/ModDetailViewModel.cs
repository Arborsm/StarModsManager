using System.Text;
using StardewModdingAPI;
using StarModsManager.Assets;
using StarModsManager.Mods;

namespace StarModsManager.ViewModels.Customs;

public class ModDetailViewModel : ViewModelBase
{
    public ModDetailViewModel(IManifest mod, string? addition = null)
    {
        Mod = GetText(mod);
        Addition = addition;
    }

    public ModDetailViewModel(OnlineMod onlineMod, string? addition = null)
    {
        Mod = GetText(onlineMod);
        Addition = addition;
    }

    public string Mod { get; }
    public string? Addition { get; }

    private static string YesOrNo(bool value)
    {
        return value ? Lang.Yes : Lang.No;
    }

    private static string GetText(OnlineMod onlineMod)
    {
        var stringBuilder = new StringBuilder();
        
        stringBuilder.Append($"{Lang.ModName}:  {onlineMod.Title}\n");
        stringBuilder.Append($"{Lang.ModNexusId}:  {onlineMod.ModId}\n");
        if (onlineMod.Description != null) 
            stringBuilder.Append($"{Lang.ModDescription}:  {onlineMod.Description}\n");
        if (onlineMod.Author != null) 
            stringBuilder.Append($"{Lang.ModAuthor}:  {onlineMod.Author}\n");
        if (onlineMod.Version != null) 
            stringBuilder.Append($"{Lang.ModLatestVersion}:  {onlineMod.Version}\n");
        
        return stringBuilder.ToString();
    }

    private static string GetText(IManifest mod)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append($"{Lang.ModName}:  {mod.Name}\n");
        stringBuilder.Append($"{Lang.ModUniqueId}:  {mod.UniqueID}\n");
        stringBuilder.Append($"{Lang.ModDescription}:  {mod.Description}\n");
        stringBuilder.Append($"{Lang.ModAuthor}:  {mod.Author}\n");
        stringBuilder.Append($"{Lang.ModVersion}:  {mod.Version}\n");
        if (mod.UpdateKeys.Length > 0)
        {
            stringBuilder.Append(Lang.ModUpdateKeys + ":  ");
            foreach (var update in mod.UpdateKeys) stringBuilder.Append($"[{update}]");
            stringBuilder.AppendLine();
        }

        if (mod.ContentPackFor != null)
        {
            stringBuilder.AppendLine();
            stringBuilder.Append($"{string.Format(Lang.IsContentPackFor, mod.ContentPackFor.UniqueID)}\n");
        }

        if (mod.Dependencies.Length > 0)
        {
            stringBuilder.AppendLine();
            stringBuilder.Append($"{Lang.Dependencies}:\n");
            foreach (var dependency in mod.Dependencies)
            {
                stringBuilder.Append($"--{Lang.ModUniqueId}:  {dependency.UniqueID}\n");
                stringBuilder.Append($"  {Lang.DependencyIsRequired}:  {YesOrNo(dependency.IsRequired)}\n");
                if (dependency.MinimumVersion != null)
                    stringBuilder.Append($"  {Lang.DependencyMinVersion}:  {dependency.MinimumVersion}\n");
            }
        }

        return stringBuilder.ToString();
    }
}