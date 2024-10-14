using CommunityToolkit.Mvvm.ComponentModel;

namespace StarModsManager.Common.Config;

public partial class TransConfig : ConfigBase
{
    [ObservableProperty]
    private string _apiSelected  = "OpenAI";
    [ObservableProperty]
    private string _language = string.Empty;
    [ObservableProperty]
    private string _promptText  = string.Empty;
    [ObservableProperty]
    private bool _isBackup = true;
    [ObservableProperty]
    private bool _isTurbo = true;
    [ObservableProperty]
    private int _delayMs = 500;
}