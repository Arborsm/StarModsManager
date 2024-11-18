using System.Text.Json.Serialization.Metadata;

namespace StarModsManager.Config;

public class TransConfig : ConfigBase
{
    public string ApiSelected
    {
        get;
        set => SetProperty(ref field, value);
    } = "OpenAI";

    public int DelayMs
    {
        get;
        set => SetProperty(ref field, value);
    } = 500;

    public bool IsBackup
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    public bool IsTurbo
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    public string Language
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string PromptText
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    protected override JsonTypeInfo GetJsonTypeInfo()
    {
        return ConfigContent.Default.TransConfig;
    }
    
    public static TransConfig LoadOrCreate()
    {
        var config = Load<TransConfig>(ConfigContent.Default.TransConfig);
        return config ?? new TransConfig();
    }
}