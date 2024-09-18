using Newtonsoft.Json;

namespace StarModsManager.Common.Config;

public class TransConfig
{
    public string ApiSelected { get; set; } = "OpenAI";
    public string Language { get; set; } = "zh";

    public string EnToCn { get; set; } = "你是一个游戏文本翻译助手，这次要翻译的是星露谷物语的一个模组，" +
                                         "贴合游戏将英文意译成简体中文并加以润色使其更符合中文的语言环境，" +
                                         "意译输入文本，不要直接使用原参考文本, " +
                                         "符号组合不需要翻译并务必保留在语句对应的位置，以输入格式直接输出结果。";

    [JsonIgnore] public TransApiConfig TransApiConf { get; set; } = new();

    public bool IsBackup { get; set; } = true;
}