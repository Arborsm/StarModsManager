using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarModsManager.Common.Main;
using StarModsManager.Common.Trans;
using StarDebug = StarModsManager.Api.StarDebug;

namespace StarModsManager.ViewModels.Customs;

public partial class ApiSettingViewModel(string selectedApi) : ViewModelBase
{
    public bool IsApiKeyEnabled => Translator.Instance.CurrentTranslator.NeedApi;
    [ObservableProperty]
    private string _apiKey = Services.TransApiConfigs[selectedApi].Api;
    [ObservableProperty]
    private string _url = Services.TransApiConfigs[selectedApi].Url;
    [ObservableProperty]
    private string _model = Services.TransApiConfigs[selectedApi].Model;
    public ObservableCollection<string> Models { get; set; } = new(Services.TransApiConfigs[selectedApi].Models);

    partial void OnApiKeyChanged(string? oldValue, string newValue)
    {
        if (newValue != oldValue) Services.TransApiConfigs[selectedApi].Api = newValue;
    }

    partial void OnUrlChanged(string? oldValue, string newValue)
    {
        if (newValue != oldValue) Services.TransApiConfigs[selectedApi].Url = newValue;
    }
    
    partial void OnModelChanged(string? oldValue, string newValue)
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
                await Translator.Instance.CurrentTranslator.GetSupportModelsAsync(Services.TransApiConfigs[selectedApi]);
            models.ForEach(Models.Add);
            if (Models.Count > 0) Model = Models.First();
        }
        catch (Exception? e)
        {
            StarDebug.Error(e);
        }
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