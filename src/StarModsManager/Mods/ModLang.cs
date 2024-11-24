using CommunityToolkit.Mvvm.ComponentModel;

namespace StarModsManager.Mods;

public partial class ModLang(string key, string? sourceLang, string? targetLang, bool? isMisMatch = false)
    : ObservableObject
{
    [ObservableProperty]
    public partial bool? IsMisMatch { get; set; } = isMisMatch;

    [ObservableProperty]
    public partial string? TargetLang { get; set; } = targetLang;

    public string Key { get; } = key;
    public string? SourceLang { get; } = sourceLang;
}