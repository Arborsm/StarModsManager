using System.Text.Json.Serialization.Metadata;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StarModsManager.Common.Config;

public partial class TransConfig : ConfigBase
{
    [ObservableProperty]
    private string _apiSelected = "OpenAI";

    [ObservableProperty]
    private int _delayMs = 500;

    [ObservableProperty]
    private bool _isBackup = true;

    [ObservableProperty]
    private bool _isTurbo = true;

    [ObservableProperty]
    private string _language = string.Empty;

    [ObservableProperty]
    private string _promptText = string.Empty;
    
    protected override JsonTypeInfo GetJsonTypeInfo()
    {
        return ConfigContent.Default.TransConfigContent;
    }

    protected override IConfigContent GetContent()
    {
        return new TransConfigContent
        {
            ApiSelected = this.ApiSelected,
            DelayMs = this.DelayMs,
            IsBackup = this.IsBackup,
            IsTurbo = this.IsTurbo,
            Language = this.Language,
            PromptText = this.PromptText
        };
    }

    protected override void LoadFromJson(object loadedConfig)
    {
        if (loadedConfig is not TransConfigContent config) return;
        this.ApiSelected = config.ApiSelected;
        this.DelayMs = config.DelayMs;
        this.IsBackup = config.IsBackup;
        this.IsTurbo = config.IsTurbo;
        this.Language = config.Language;
        this.PromptText = config.PromptText;
    }
}

public class TransConfigContent : IConfigContent
{
    public string ApiSelected { get; set; } = string.Empty;
    public int DelayMs { get; set; }
    public bool IsBackup { get; set; }
    public bool IsTurbo { get; set; }
    public string Language { get; set; } = string.Empty;
    public string PromptText { get; set; } = string.Empty;
}