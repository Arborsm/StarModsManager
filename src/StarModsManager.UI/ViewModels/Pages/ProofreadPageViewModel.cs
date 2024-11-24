using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarModsManager.Api;
using StarModsManager.Config;
using StarModsManager.Lib;
using StarModsManager.Mods;
using StarModsManager.Trans;
using TranslationContext = StarModsManager.Trans.TranslationContext;

namespace StarModsManager.ViewModels.Pages;

public partial class ProofreadPageViewModel : MainPageViewModelBase
{
    private static Dictionary<string, (string?, string?)> _langMap = null!;
    private readonly Dictionary<string, (string?, string?)> _langEditedCache = [];

    private int _filterIndex;
    private List<ModLang> _filterList = [];

    private IReadOnlyList<ModLang> _langList = [];

    public ProofreadPageViewModel()
    {
        if (!ViewModelService.IsInDesignMode)
        {
            var currentMod = I18NMods.FirstOrDefault();
            if (currentMod != null) CurrentMod = currentMod;
        }
        else
        {
            _langList = ExampleData.All;
            UpdateFilter();
        }
    }

    public ProofreadConfig ProofreadConfig => Services.ProofreadConfig;

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial LocalMod? CurrentMod { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowMods))]
    public partial bool IsFilter { get; set; } = Services.ProofreadConfig.IsFilter;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial bool IsNotSave { get; set; }

    [ObservableProperty]
    public partial ModLang? SelectedItem { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    public partial string? SearchText { get; set; }

    public override string NavHeader => NavigationService.Check;
    public DataGridCollectionView ModLangsView => new(_langList);
    private static IEnumerable<LocalMod> I18NMods => ModsHelper.Instance.I18LocalMods.OrderBy(it => it.Manifest.Name);
    private static IEnumerable<LocalMod> MisMatchMods => I18NMods.Where(it => it.LazyIsMisMatch.Value is true or null);
    public ObservableCollection<LocalMod> ShowMods => new(IsFilter ? MisMatchMods : I18NMods);
    private bool IsSearchTextNotNull => !string.IsNullOrEmpty(SearchText);

    public void AddEditedLang(ModLang item)
    {
        if (item.TargetLang == _langMap[item.Key].Item2) return;
        _langEditedCache[item.Key] = (item.SourceLang, item.TargetLang);
        IsNotSave = _langEditedCache.Count != 0;
    }

    partial void OnIsFilterChanged(bool value)
    {
        Services.ProofreadConfig.IsFilter = value;
        UpdateFilter();
    }

    private void UpdateFilter()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (Services.ProofreadConfig.IsFilter)
                ModLangsView.Filter = item =>
                {
                    var modLang = (ModLang)item;
                    return modLang.IsMisMatch is true or null;
                };
            else
                ModLangsView.Filter = _ => true;
        });
    }

    [RelayCommand(CanExecute = nameof(IsSearchTextNotNull))]
    private void Search()
    {
        _filterIndex = 0;
        _filterList = _langList
            .Where(item =>
            {
                var s = item.SourceLang?.Contains(SearchText!, StringComparison.OrdinalIgnoreCase) ?? false;
                var t = item.TargetLang?.Contains(SearchText!, StringComparison.OrdinalIgnoreCase) ?? false;
                return item.Key.Contains(SearchText!, StringComparison.OrdinalIgnoreCase) || s || t;
            })
            .ToList();
        if (_filterList.Count == 0) return;
        SelectedItem = _filterList[_filterIndex];
    }

    [RelayCommand]
    private void NextSearch()
    {
        if (_filterList.Count == 0) return;
        _filterIndex = (_filterIndex + 1) % _filterList.Count;
        SelectedItem = _filterList[_filterIndex];
    }

    [RelayCommand]
    private void PrevSearch()
    {
        if (_filterList.Count == 0) return;
        _filterIndex = (_filterIndex - 1 + _filterList.Count) % _filterList.Count;
        SelectedItem = _filterList[_filterIndex];
    }

    [RelayCommand]
    private async Task TranslateAsync()
    {
        if (SelectedItem?.SourceLang != null)
        {
            var result = await Translator.Instance.TranslateTextAsync(SelectedItem.SourceLang);
            if (string.IsNullOrEmpty(result)) return;
            SelectedItem.TargetLang = result;
            SelectedItem.IsMisMatch = SelectedItem.SourceLang.IsMisMatch(SelectedItem.TargetLang);
            AddEditedLang(SelectedItem);
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotSave))]
    private async Task SaveAsync()
    {
        if (CurrentMod is null) return;
        var (_, targetLang) = CurrentMod.ReadMap(Services.TransConfig.Language);
        foreach (var kv in _langEditedCache)
        {
            _langMap[kv.Key] = kv.Value;
            targetLang[kv.Key] = kv.Value.Item2 ?? string.Empty;
        }

        try
        {
            await File.WriteAllTextAsync(CurrentMod.PathS + "\\i18n\\" + $"{Services.TransConfig.Language}.json",
                TranslationContext.GetJson(targetLang));
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

    partial void OnCurrentModChanged(LocalMod? oldValue, LocalMod? newValue)
    {
        IsLoading = true;
        if (Equals(newValue, oldValue) || newValue is null) return;
        _langMap = newValue.LoadLangMap();
        _langList = _langMap.Select(x =>
        {
            var sourceText = x.Value.Item1;
            var targetText = x.Value.Item2;
            var isMisMatch = sourceText.IsMisMatch(targetText);
            return new ModLang(x.Key, sourceText, targetText, isMisMatch);
        }).ToList();
        UpdateFilter();
        IsLoading = false;
    }

    private static class ExampleData
    {
        private static IReadOnlyList<ModLang>? _all;
        internal static IReadOnlyList<ModLang> All => _all ??= CreateList().AsReadOnly();

        private static List<ModLang> CreateList()
        {
            return
            [
                new ModLang("key", "sodddddddddssssssssssssssssssssssssddurce", "target"),
                new ModLang("key2", "sourcedgywiagydgwayugduywagdyugawyugduyawgdyugawyugdyuawgdyuwadugauywgd2",
                    "的女娃把电脑架空挖宝多看几遍科技部不断加快把我看不懂就看我把大家可别挖掘被大家挖宝角度看八戒悟空不断加快瓦伯爵挖矿机不断加快挖不到就看不挖基坑底部我到北京网卡绑定控件被挖", true),
                new ModLang("key3", "souaaaaaaaaaaaaaaaaaaaaaaaaaaaarce3",
                    "wdawnjdnjwakkkkkkkkkkkkkkkkndjakwndjkawnjkdnjawkndjkwanjdnjwakndjanwjdnjwandjanwjdnjkwandjknwajkdnjakw",
                    null)
            ];
        }
    }
}