using StarModsManager.Common.Main;
using StarModsManager.Common.Trans;

namespace StarModsManager.ViewModels.Customs;

public class TransSettingViewModel
{
    public List<string> Apis { get; } = Translator.Instance.Apis.Select(it => it.Name).ToList();
    public List<string> Langs { get; } = [];
    public string SelectedLang { get; set; } = Services.TransConfig.Language;
    public string SelectedApi { get; set; } = Services.TransConfig.ApiSelected;
    public string PromptText { get; set; } = Services.TransConfig.PromptText;
    public bool IsBackup { get; set; } = Services.TransConfig.IsBackup;
}