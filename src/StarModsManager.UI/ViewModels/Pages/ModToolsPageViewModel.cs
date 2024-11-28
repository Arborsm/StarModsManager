using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarModsManager.Api;
using StarModsManager.Api.NexusMods;
using StarModsManager.Assets;
using StarModsManager.Lib;
using StarModsManager.Trans;

namespace StarModsManager.ViewModels.Pages;

public partial class ModToolsPageViewModel : MainPageViewModelBase
{
    public override string NavHeader => NavigationService.ModTools;

    [ObservableProperty]
    public partial string NexusApiState { get; set; } =
        NexusManager.IsAvailable ? Lang.Available : Lang.Unavailable;

    [ObservableProperty]
    public partial string NexusCookieState { get; set; } =
        NexusDownload.Instance.IsNoCookie ? Lang.Unavailable : Lang.Available;

    [ObservableProperty]
    public partial string TranslateServiceState { get; set; } =
        Translator.Instance.IsAvailable ? Lang.Available : Lang.Unavailable;

    [ObservableProperty]
    public partial bool WithPrompt { get; set; }

    [ObservableProperty]
    public partial string? Message { get; set; }

    [ObservableProperty]
    public partial string? Response { get; set; }

    [RelayCommand]
    private async Task RefreshNexusApiStateAsync()
    {
        await NexusManager.Test();
        NexusApiState = NexusManager.IsAvailable ? Lang.Available : Lang.Unavailable;
        Services.Notification?.Show(Lang.NexusApiState + " " + NexusApiState);
    }

    [RelayCommand]
    private async Task RefreshNexusCookieStateAsync()
    {
        NexusDownload.Instance.Reset();
        await NexusDownload.Instance.EnsureInitializedAsync();
        NexusCookieState = NexusDownload.Instance.IsNoCookie ? Lang.Unavailable : Lang.Available;
        Services.Notification?.Show(Lang.NexusCookieState + " " + NexusCookieState);
    }

    [RelayCommand]
    private async Task RefreshTranslateServiceStateAsync()
    {
        await Translator.Instance.Test();
        TranslateServiceState = Translator.Instance.IsAvailable ? Lang.Available : Lang.Unavailable;
        Services.Notification?.Show(Lang.TranslateServiceState + " " + TranslateServiceState);
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        if (string.IsNullOrEmpty(Message)) return;
        if (WithPrompt)
            Response = await Translator.Instance.TranslateTextAsync(Message);
        else
            Response = await Translator.Instance.CurrentTranslator.StreamCallWithMessageAsync(Message, "user",
                Services.TransApiConfigs[Services.TransConfig.ApiSelected], CancellationToken.None);
    }
}