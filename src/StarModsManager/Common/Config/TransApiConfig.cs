using System.Text.Json.Serialization.Metadata;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StarModsManager.Common.Config;

public partial class TransApiConfig(string additionName) : ConfigBase(additionName)
{
    [ObservableProperty]
    private string _api = "your-api-key";

    [ObservableProperty]
    private string _model = "gpt-3.5-turbo";

    [ObservableProperty]
    private string[] _models = [];

    [ObservableProperty]
    private string _url = "https://api.openai.com";
    
    protected override JsonTypeInfo GetJsonTypeInfo()
    {
        return ConfigContent.Default.TransApiConfigContent;
    }

    protected override IConfigContent GetContent()
    {
        return new TransApiConfigContent
        {
            Api = this.Api,
            Model = this.Model,
            Models = this.Models,
            Url = this.Url
        };
    }

    protected override void LoadFromJson(object loadedConfig)
    {
        if (loadedConfig is not TransApiConfigContent config) return;
        this.Api = config.Api;
        this.Model = config.Model;
        this.Models = config.Models;
        this.Url = config.Url;
    }
}

public class TransApiConfigContent : IConfigContent
{
    public string Api { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string[] Models { get; set; } = [];
    public string Url { get; set; } = string.Empty;
}