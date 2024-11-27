using System.Collections.ObjectModel;
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

    public bool IsApiKeyEnabled => Translator.Instance.CurrentTranslator.NeedApi;
    public ObservableCollection<string> Models { get; set; } = new(Services.TransApiConfigs[selectedApi].Models);

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

    [RelayCommand]
    private async Task GetModelsAsync()
    {
        try
        {
            Models.Clear();
            var models =
                await Translator.Instance.CurrentTranslator.GetSupportModelsAsync(
                    Services.TransApiConfigs[selectedApi]);
            models.ForEach(Models.Add);
            if (Models.Count > 0) Model = Models.First();
        }
        catch (Exception? e)
        {
            SMMDebug.Error(e);
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await Task.Run(Translator.Instance.Test);
    }

    [RelayCommand]
    private void Save()
    {
        var apiConfig = Services.TransApiConfigs[selectedApi];
        apiConfig.Api = ApiKey;
        apiConfig.Url = Url;
        apiConfig.Model = Model;
        apiConfig.Models = Models.ToArray();
    }
}