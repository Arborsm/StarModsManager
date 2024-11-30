using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using StarModsManager.Api;
using StarModsManager.Api.SMAPI;
using StarModsManager.Config;
using StarModsManager.Lib;
using StarModsManager.ViewModels.Items;

namespace StarModsManager.ViewModels.Pages;

public partial class MainPageViewModel : MainPageViewModelBase, IDisposable
{
    private int _filterIndex;
    private List<ModViewModel> _filterList = [];
    private List<ModViewModel> AllMods { get; set; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModLabels))]
    [NotifyPropertyChangedFor(nameof(IsSelectedLabel))]
    [NotifyCanExecuteChangedFor(nameof(RmFromLabelCommand))]
    public partial ItemLabelViewModel? SelectedLabel { get; set; }

    [ObservableProperty]
    public partial ModViewModel? SelectedMod { get; set; }

    public MainConfig Config => Services.MainConfig;
    public List<ModViewModel> SelectedMods { get; set; } = [];
    public override string NavHeader => NavigationService.Main;
    public ObservableCollection<ItemLabelViewModel>? ModLabels { get; set; }
    public ObservableCollection<ModViewModel> Mods { get; } = [];
    public bool IsHiddenLabel => SelectedLabel?.Title == ItemLabelViewModel.Hidden;
    public bool IsSelectedLabel => SelectedLabel != null;

    [ObservableProperty]
    public partial string? SearchText { get; set; }

    public void Dispose()
    {
        if (ModLabels != null) ModLabels.CollectionChanged -= ModLabelsOnCollectionChanged;
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private void Search()
    {
        if (SearchText == null) return;
        _filterIndex = 0;
        _filterList = Mods.Where(mod =>
            mod.LocalMod!.Manifest.Name.Contains(SearchText) ||
            mod.LocalMod!.Manifest.Description.Contains(SearchText) ||
            mod.LocalMod!.Manifest.Author.Contains(SearchText)).ToList();
        if (_filterList.Count == 0) return;
        SelectedMod = _filterList[_filterIndex];
    }

    [RelayCommand]
    private void NextSearch()
    {
        if (_filterList.Count == 0) return;
        _filterIndex = (_filterIndex + 1) % _filterList.Count;
        SelectedMod = _filterList[_filterIndex];
    }

    [RelayCommand]
    private void PrevSearch()
    {
        if (_filterList.Count == 0) return;
        _filterIndex = (_filterIndex - 1 + _filterList.Count) % _filterList.Count;
        SelectedMod = _filterList[_filterIndex];
    }

    private void SaveLabels()
    {
        var list = ModLabels?.Select(it => new TitleOnlyViewModel(it)).ToList();
        var json = JsonSerializer.Serialize(list, TitleOnlyContext.Default.ListTitleOnlyViewModel);
        var saveJson = string.Empty;
        var filePath = GetFilePath();
        if (File.Exists(filePath)) saveJson = File.ReadAllText(filePath);
        if (saveJson != json) File.WriteAllText(filePath, json);
    }

    private void UpdateMods() => Dispatcher.UIThread.Invoke(() =>
    {
        Mods.Clear();
        if (ModLabels != null && ModLabels.Any(label => label.IsSelected))
        {
            ModLabels
                .Where(label => label.IsSelected)
                .SelectMany(label => label.Items)
                .Where(mod => ModLabels.Where(label => label.IsSelected).All(label => label.Items.Contains(mod)))
                .Distinct()
                .ForEach(Mods.Add);
        }
        else
        {
            AllMods
                .Where(mod =>
                    !ModLabels?.First(label => label.Title == ItemLabelViewModel.Hidden).Items.Contains(mod) ?? true)
                .Where(mod => !Mods.Contains(mod)).ForEach(Mods.Add);
        }
    });

    private static string GetFilePath(string fileName = "list") =>
        Path.Combine(Services.ModLabelsPath, fileName + ".json");

    [RelayCommand]
    private void SwitchAllMod() => SelectedLabel!.Items.ForEach(it => it.SwitchModCommand.Execute(null));

    [RelayCommand]
    private void AddToLabel(string label) => SelectedMods.ToArray().ForEach(mod =>
    {
        var mods = ModLabels?.First(it => it.Title == label).Items ?? [];
        if (!mods.Contains(mod)) mods.Add(mod);
        if (label == ItemLabelViewModel.Hidden && !ModLabels!.Any(model => model.IsSelected)) Mods.Remove(mod);
    });

    [RelayCommand(CanExecute = nameof(IsSelectedLabel))]
    private void RmFromLabel() => SelectedMods.ToArray().ForEach(mod =>
    {
        if (ModLabels!.Any(model => model.IsSelected)) Dispatcher.UIThread.Invoke(() => Mods.Remove(mod));
        ModLabels?.Where(label => label.IsSelected).ForEach(label => label.Items.Remove(mod));
    });

    [RelayCommand]
    private void Rename()
    {
        if (SelectedLabel is null || SelectedLabel.Title == ItemLabelViewModel.Hidden) return;
        SelectedLabel.IsEditing = true;
    }

    [RelayCommand]
    private async Task AddModAsync()
    {
        var files =
            await Services.Dialog.ShowPickupFilesDialogAsync("Select Mods to Add", true);
        if (files.Count == 0) return;
        var localPaths = files
            .Select(it => it.TryGetLocalPath())
            .Where(s => !string.IsNullOrEmpty(s))
            .Cast<string>();
        SmapiModInstaller.Install(localPaths);
    }

    [RelayCommand]
    private void RemoveLabel()
    {
        if (SelectedLabel is null) return;
        SelectedLabel.PropertyChanged -= ModLabelViewModelOnPropertyChanged;
        ModLabels?.Remove(SelectedLabel);
    }

    [RelayCommand]
    private void AddLabel() =>
        ModLabels?.Add(new ItemLabelViewModel($"Label{ModLabels?.Count + 1}", [], Colors.Aqua));

    [RelayCommand]
    private void Refresh()
    {
        Task.Run(LoadModsAsync);
    }


    [RelayCommand]
    private void OpenBackupFolder()
    {
        var path = Directory.GetParent(Services.BackupPath)?.FullName;
        if (path is null) return;
        PlatformHelper.OpenFileOrUrl(path);
    }

    private void ModLabelViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ItemLabelViewModel.IsSelected)) UpdateMods();
        SaveLabels();
    }

    private void ModLabelsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => SaveLabels();

    public async Task LoadModsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        await ModsHelper.Instance.FindModsAsync();
        Dispatcher.UIThread.Invoke(() =>
        {
            IsLoading = true;
            Mods.Clear();
        });
        AllMods = ModsHelper.Instance.LocalModsMap.Values
            .Select(it => new ModViewModel(it.OnlineMod, it))
            .OrderBy(it => it.OnlineMod.Title)
            .ToList();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ViewModelService.Resolve<MainViewModel>().MissingDependencyModsCount =
                AllMods.Count(it => it.IsDependencyMissing);
            IsLoading = false;
        });
        stopwatch.Stop();
        Log.Information("Loaded {Count} mods in {Cost}ms", Mods.Count, stopwatch.ElapsedMilliseconds);
        _ = Task.WhenAll(AllMods.AsParallel().Select(async mod => await mod.LoadCoverAsync()));
        InitLabels();
    }

    private void InitLabels()
    {
        List<ItemLabelViewModel>? list = null;
        if (File.Exists(GetFilePath()))
            list = JsonSerializer
                .Deserialize<List<TitleOnlyViewModel>>(File.ReadAllText(GetFilePath()),
                    TitleOnlyContext.Default.ListTitleOnlyViewModel)?
                .Select(model => new ItemLabelViewModel(model.Title, AllMods, Colors.LightBlue))
                .ToList();
        Dispatcher.UIThread.Invoke(() =>
        {
            if (list != null && list.All(item => item.Title != ItemLabelViewModel.Hidden))
                list?.Add(new ItemLabelViewModel());
            ModLabels = list is null || list.Count < 1
                ? [new()]
                : new ObservableCollection<ItemLabelViewModel>(list);
            ModLabels.CollectionChanged += ModLabelsOnCollectionChanged;
            ModLabels.ForEach(vm => vm.PropertyChanged += ModLabelViewModelOnPropertyChanged);
            SelectedLabel = ModLabels[0];
            ModLabels.First(model => model.Title == ItemLabelViewModel.Hidden).Items.ForEach(mod => Mods.Remove(mod));
            UpdateMods();
        });
    }

    public void ReFreshLabels()
    {
        if (ModLabels is null) return;
        var list = ModLabels.ToList();
        ModLabels.Clear();
        list.Select(label => new ItemLabelViewModel(label.Title, label.Items, label.BorderColor))
            .ForEach(vm => Dispatcher.UIThread.Invoke(() => ModLabels.Add(vm)));
    }
}