using System.Text.Json.Serialization.Metadata;
using Newtonsoft.Json;

namespace StarModsManager.Config;

public class TransApiConfig : ConfigBase
{
    public TransApiConfig(string additionName)
    {
        if (!string.IsNullOrEmpty(additionName))
            AdditionNameJson = additionName;
    }

    [JsonConstructor]
    public TransApiConfig() : this(string.Empty)
    {
    }

    public string AdditionNameJson { get; set; } = null!;

    public string Api
    {
        get;
        set => SetProperty(ref field, value);
    } = "your-api-key";

    public string Model
    {
        get;
        set => SetProperty(ref field, value);
    } = "gpt-3.5-turbo";

    public string[] Models
    {
        get;
        set => SetProperty(ref field, value);
    } = [];

    public string Url
    {
        get;
        set => SetProperty(ref field, value);
    } = "api.openai.com";

    protected override string AdditionName()
    {
        return AdditionNameJson;
    }

    protected override JsonTypeInfo GetJsonTypeInfo()
    {
        return ConfigContent.Default.TransApiConfig;
    }

    public static TransApiConfig LoadOrCreate(string additionName)
    {
        var config = Load<TransApiConfig>(ConfigContent.Default.TransApiConfig, additionName);
        config ??= new TransApiConfig(additionName);
        config.IsLoaded = true;
        return config;
    }
}