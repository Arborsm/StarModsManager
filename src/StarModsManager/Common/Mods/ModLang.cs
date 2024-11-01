using CommunityToolkit.Mvvm.ComponentModel;

namespace StarModsManager.Common.Mods;

public partial class ModLang(string key, string? sourceLang, string? targetLang, bool? isMatch = true)
    : ObservableObject
{
    [ObservableProperty]
    private bool? _isMatch = isMatch;

    [ObservableProperty]
    private string? _targetLang = targetLang;

    public string Key { get; } = key;
    public string? SourceLang { get; } = sourceLang;
}