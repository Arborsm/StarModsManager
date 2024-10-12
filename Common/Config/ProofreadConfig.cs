using CommunityToolkit.Mvvm.ComponentModel;

namespace StarModsManager.Common.Config;

public partial class ProofreadConfig : ConfigBase
{
    [ObservableProperty]
    private bool _isVisibleHeader = true;
    [ObservableProperty]
    private bool _enableHeaderResizing;
    [ObservableProperty]
    private bool _canSort;
    [ObservableProperty]
    private bool _hasBorder;
    [ObservableProperty]
    private bool _isFilter;
}