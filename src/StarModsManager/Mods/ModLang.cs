using CommunityToolkit.Mvvm.ComponentModel;

namespace StarModsManager.Mods;

public partial class ModLang(string key, string? sourceLang, string? targetLang, bool? isMisMatch = false)
    : ObservableObject
{
    [ObservableProperty]
    private bool? _isMisMatch = isMisMatch;

    [ObservableProperty]
    private string? _targetLang = targetLang;

    public string Key { get; } = key;
    public string? SourceLang { get; } = sourceLang;
}