using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using StarModsManager.Api;
using StarModsManager.Common.Main;
using StarModsManager.Common.Mods;
using StarModsManager.Common.Trans;

namespace StarModsManager.ViewModels.Pages;

public partial class TransPageViewModel : ViewModelBase
{
    public TransPageViewModel()
    {
        // if (Services.IsInDesignMode)
        // {
        //     _toTansMod = [];
        //     Mods = new ObservableCollection<ToTansMod>([]);
        //     return;
        // }
        _toTansMod =
            ModData.Instance.I18LocalMods.Where(it => it.GetUntranslatedMap().Count > 0).ToArray();
        Mods = new ObservableCollection<ToTansMod>(
            _toTansMod.Select(it => new ToTansMod(true, it.Name, it.GetUntranslatedMap().Count.ToString())));
    }
    private readonly LocalMod[] _toTansMod;
    [ObservableProperty]
    private int _maxProgress;
    [ObservableProperty]
    private int _progress;
    [ObservableProperty]
    private string _sourceText = string.Empty;
    [ObservableProperty]
    private string _targetText = string.Empty;
    public ObservableCollection<ToTansMod> Mods { get; }
    public CancellationTokenSource CancellationTokenSource { get; set; } = null!;
    
    public async Task Translate()
    {
        await Translator.Instance.ProcessDirectories(_toTansMod, CancellationTokenSource.Token);
    }
}

public struct ToTansMod(bool isChecked, string name, string keys)
{
    public bool IsChecked { get; set; } = isChecked;
    public string Name { get; } = name;
    public string Keys { get; } = keys;
}