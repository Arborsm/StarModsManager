using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarModsManager.Api;
using StarModsManager.Trans;

namespace StarModsManager.ViewModels.Customs;

public partial class ApiSettingViewModel(string selectedApi) : ViewModelBase
{
    [ObservableProperty]
    public partial string ApiKey { get; set; } = Services.TransApiConfigs[selectedApi].Api;

    [ObservableProperty]
    public partial string Model { get; set; } = Services.TransApiConfigs[selectedApi].Model;

    [ObservableProperty]
    public partial string Url { get; set; } = Services.TransApiConfigs[selectedApi].Url;

    [ObservableProperty]
    public partial string[] Models { get; set; } = Services.TransApiConfigs[selectedApi].Models;

    public bool IsApiKeyEnabled => Translator.Instance.CurrentTranslator.NeedApi;

    partial void OnApiKeyChanged(string oldValue, string newValue)
    {
        if (newValue != oldValue) Services.TransApiConfigs[selectedApi].Api = newValue;
    }

    partial void OnUrlChanged(string oldValue, string newValue)
    {
        if (newValue != oldValue) Services.TransApiConfigs[selectedApi].Url = newValue;
    }

    partial void OnModelChanged(string oldValue, string newValue)
    {
        if (newValue != oldValue) Services.TransApiConfigs[selectedApi].Model = newValue;
    }

    partial void OnModelsChanged(string[] oldValue, string[] newValue)
    {
        if (newValue != oldValue) Services.TransApiConfigs[selectedApi].Models = newValue;
    }

    [RelayCommand]
    private async Task GetModelsAsync()
    {
        try
        {
            var models =
                await Translator.Instance.CurrentTranslator.GetSupportModelsAsync(
                    Services.TransApiConfigs[selectedApi]);
            Models = models.ToArray();
            Model = Models.First();
        }
        catch (Exception? e)
        {
            SMMDebug.Error(e);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await Task.Run(Translator.Instance.Test);
    }

    [RelayCommand]
    private void Save()
    {
    }
}