using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarModsManager.Api;
using StarModsManager.Common.Trans;

namespace StarModsManager.ViewModels.Customs;

public partial class TransSettingViewModel : ViewModelBase
{
    private const string OpenAI = "OpenAI";

    [ObservableProperty]
    private int _delayMs = Services.TransConfig.DelayMs;

    [ObservableProperty]
    private bool _isTurbo = Services.TransConfig.IsTurbo;

    [ObservableProperty]
    private string _promptText = Services.TransConfig.PromptText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanTurboEnabled))]
    private string _selectedApi = Services.TransConfig.ApiSelected;

    [ObservableProperty]
    private string _showLang = SMMHelper.SwitchLanguage(Services.TransConfig.Language);

    public List<string> Apis { get; } = Translator.Instance.Apis.Select(it => it.Name).ToList();
    public List<string> Langs { get; } = SMMHelper.LanguageMap.Keys.ToList();
    public bool CanTurboEnabled => SelectedApi == OpenAI;
    private string SelectedLang { get; set; } = Services.TransConfig.Language;
    public bool IsBackup { get; set; } = Services.TransConfig.IsBackup;

    partial void OnShowLangChanged(string? oldValue, string newValue)
    {
        var lang = SMMHelper.SwitchLanguage(newValue);
        if (string.IsNullOrEmpty(PromptText) && lang == "zh")
            PromptText = "你是一位专业的游戏本地化翻译专家,擅长将《星露谷物语》及其模组的英文文本翻译成地道的简体中文。请遵循以下指引:\n" +
                         "深入理解原文含义和游戏风格,采用意译方式将英文内容翻译成通顺自然的中文表达。\n" +
                         "确保翻译贴合《星露谷物语》的游戏背景和语境,使用符合游戏氛围的措辞和语言。\n" +
                         "适当润色译文,使其更加流畅自然,符合中文的表达习惯。\n" +
                         "保持游戏特有术语的一致性,参考官方中文版的惯例。\n" +
                         "准确传达原文的幽默感、情感色彩和文化内涵。\n" +
                         "所有符号组合(如 %noun等)保留在原文对应的位置,无需翻译。\n" +
                         "避免直接使用机器翻译或照搬参考文本,力求创造性翻译。\n" +
                         "输出格式应与输入保持一致。\n" +
                         "请以上述要求为指引,将给定的英文游戏文本翻译成优质的简体中文版本。";
        if (oldValue == newValue) return;
        SelectedLang = lang;
    }

    partial void OnSelectedApiChanged(string? oldValue, string newValue)
    {
        if (newValue != oldValue) Services.TransConfig.ApiSelected = newValue;
        if (newValue != OpenAI) IsTurbo = false;
    }

    [RelayCommand]
    private void Save()
    {
        Services.TransConfig.IsBackup = IsBackup;
        Services.TransConfig.ApiSelected = SelectedApi;
        Services.TransConfig.Language = SelectedLang;
        Services.TransConfig.PromptText = PromptText;
        Services.TransConfig.DelayMs = DelayMs;
        Services.TransConfig.IsTurbo = IsTurbo;
    }
}