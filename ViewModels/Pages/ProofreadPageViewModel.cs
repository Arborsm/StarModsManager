using System.Collections.ObjectModel;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using StarModsManager.Api;
using StarModsManager.Common.Main;
using StarModsManager.Common.Mods;
using StarModsManager.Common.Trans;

// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable ClassNeverInstantiated.Global

namespace StarModsManager.ViewModels.Pages;

public partial class ProofreadPageViewModel : MainPageViewModelBase
{
    private static Dictionary<string, (string?, string?)> _langMap = null!;
    private readonly Dictionary<string, (string?, string?)> _langEditedCache = [];
    [ObservableProperty]
    private ModLang? _selectedItem;
    [ObservableProperty]
    private LocalMod _currentMod = null!;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isNotSave;
    [ObservableProperty]
    private bool _isFilter = Services.ProofreadConfig.IsFilter;
    [ObservableProperty]
    private bool _showLines = Services.ProofreadConfig.HasBorder;
    [ObservableProperty]
    private bool _isVisibleHeader = Services.ProofreadConfig.IsVisibleHeader;
    [ObservableProperty]
    private bool _canSort = Services.ProofreadConfig.CanSort;
    [ObservableProperty]
    private bool _canResize = Services.ProofreadConfig.EnableHeaderResizing;

    public DataGridCollectionView ModLangsView { get; set; } = null!;
    
    public static ObservableCollection<LocalMod> I18NMods { get; } = 
        [..ModsHelper.Instance.I18LocalMods.OrderBy(it => it.Name)];

    public void AddEditedLang(ModLang item)
    {
        if (item.TargetLang == _langMap[item.Key].Item2) return;
        _langEditedCache[item.Key] = (item.SourceLang, item.TargetLang);
        IsNotSave = _langEditedCache.Count != 0;
    }

    partial void OnShowLinesChanged(bool value)
    {
        Services.ProofreadConfig.HasBorder = value;
    }

    partial void OnIsVisibleHeaderChanged(bool value)
    {
        Services.ProofreadConfig.IsVisibleHeader = value;
    }

    partial void OnIsFilterChanged(bool value)
    {
        Services.ProofreadConfig.IsFilter = value;
        if (value)
        {
            ModLangsView.Filter = item =>
            {
                var modLang = (ModLang)item;
                return modLang.IsMatch != true;
            };
        } 
        else
        {
            ModLangsView.Filter = _ => true;
        }
    }

    partial void OnCanSortChanged(bool value)
    {
        Services.ProofreadConfig.CanSort = value;
    }

    partial void OnCanResizeChanged(bool value)
    {
        Services.ProofreadConfig.EnableHeaderResizing = value;
    }

    [RelayCommand]
    private async Task TranslateAsync()
    {
        if (SelectedItem?.SourceLang is not null)
        {
            SelectedItem.TargetLang = await Translator.Instance.TranslateTextAsync(SelectedItem.SourceLang);
            SelectedItem.IsMatch = IsMatch(SelectedItem);
            AddEditedLang(SelectedItem);
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotSave))]
    private async Task SaveAsync()
    {
        var (_, targetLang) = CurrentMod.ReadMap(Services.TransConfig.Language);
        foreach (var kv in _langEditedCache)
        {
            _langMap[kv.Key] = kv.Value;
            targetLang[kv.Key] = kv.Value.Item2 ?? string.Empty;
        }
        
        try
        {
            await File.WriteAllTextAsync(CurrentMod.PathS + "\\i18n\\" + $"{Services.TransConfig.Language}.json",
                JsonConvert.SerializeObject(targetLang, Formatting.Indented));
        }
        catch (Exception? e)
        {
            SMMDebug.Error(e, "保存翻译失败");
        }
        finally
        {
            _langEditedCache.Clear();
            IsNotSave = false;
        }
    }

    public ProofreadPageViewModel()
    {
        if (!Services.IsInDesignMode)
        {
            var currentMod = I18NMods.FirstOrDefault();
            if (currentMod != null)
            {
                CurrentMod = currentMod;
            }
        }
        else
        {
            ModLangsView = new DataGridCollectionView(ExampleData.All);
        }
    }

    partial void OnCurrentModChanged(LocalMod value)
    {
        LoadLangMap();
    }

    private void LoadLangMap()
    {
        var (defaultLang, targetLang) = CurrentMod.ReadMap(Services.TransConfig.Language);
        
        _langMap = defaultLang.Keys.Union(targetLang.Keys)
            .ToDictionary(key => key, key => (defaultLang.GetValueOrDefault(key), targetLang.GetValueOrDefault(key)));
        
        ModLangsView = new DataGridCollectionView(_langMap
            .Select(x =>
            {
                var sourceText = x.Value.Item1;
                var targetText = x.Value.Item2;
                var isMatch = IsMatch(sourceText, targetText);

                return new ModLang(x.Key, sourceText, targetText, isMatch);
            }));
    }

    private static bool? IsMatch(ModLang selectedItem)
    {
        return IsMatch(selectedItem.SourceLang, selectedItem.TargetLang);
    }
    
    private static bool? IsMatch(string? sourceText, string? targetText)
    {
        bool? isMatch;
        if (sourceText is null || targetText is null)
        {
            isMatch = false;
        }
        else if (sourceText.IsMismatchedTokens(targetText, false))
        {
            isMatch = null;
        }
        else
        {
            isMatch = true;
        }

        return isMatch;
    }

    private static class ExampleData
    {
        private static List<ModLang> CreateList()
        {
            return
            [
                new ModLang("key", "sodddddddddssssssssssssssssssssssssddurce", "target"),
                new ModLang("key2", "sourcedgywiagydgwayugduywagdyugawyugduyawgdyugawyugdyuawgdyuwadugauywgd2",
                    "的女娃把电脑架空挖宝多看几遍科技部不断加快把我看不懂就看我把大家可别挖掘被大家挖宝角度看八戒悟空不断加快瓦伯爵挖矿机不断加快挖不到就看不挖基坑底部我到北京网卡绑定控件被挖", false),
                new ModLang("key3", "souaaaaaaaaaaaaaaaaaaaaaaaaaaaarce3",
                    "wdawnjdnjwakkkkkkkkkkkkkkkkndjakwndjkawnjkdnjawkndjkwanjdnjwakndjanwjdnjwandjanwjdnjkwandjknwajkdnjakw",
                    null)
            ];
        }

        private static IReadOnlyList<ModLang>? _all;
        internal static IReadOnlyList<ModLang> All => _all ??= CreateList().AsReadOnly();
    }
}

public partial class ModLang(string key, string? sourceLang, string? targetLang, bool? isMatch = true) : ObservableObject
{
    public string Key { get; } = key;
    public string? SourceLang { get; } = sourceLang;
    
    [ObservableProperty]
    private bool? _isMatch = isMatch;
    [ObservableProperty]
    private string? _targetLang = targetLang;
}