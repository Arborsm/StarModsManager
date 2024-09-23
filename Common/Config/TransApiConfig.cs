using CommunityToolkit.Mvvm.ComponentModel;

namespace StarModsManager.Common.Config;

public partial class TransApiConfig : ConfigBase
{
    [ObservableProperty]
    private string _api = "your-api-key";
    [ObservableProperty]
    private string _url = "https://api.openai.com";
    [ObservableProperty]
    private string _model = "gpt-3.5-turbo";
    [ObservableProperty]
    private string[] _models = [];
}