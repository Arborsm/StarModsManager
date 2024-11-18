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
using StarModsManager.Lib;
using StarModsManager.ViewModels.Items;

namespace StarModsManager.ViewModels.Pages;

public partial class MainPageViewModel : MainPageViewModelBase, IDisposable
{
    [ObservableProperty]
    private bool _isLoading = true;

    private List<ModViewModel> AllMods { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModLabels))]
    [NotifyPropertyChangedFor(nameof(IsSelectedLabel))]
    [NotifyPropertyChangedFor(nameof(ExpanderListMods))]
    [NotifyCanExecuteChangedFor(nameof(RmFromLabelCommand))]
    private ItemLabelViewModel? _selectedLabel;

    public List<ModViewModel> SelectedMods { get; set; } = [];
    public List<ModViewModel> SelectedInLabelMods { get; set; } = [];

    public async Task LoadModsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        await ModsHelper.Instance.FindModsAsync();
        IsLoading = true;
        Mods.Clear();
        AllMods = ModsHelper.Instance.LocalModsMap.Values
            //.Where(it => !string.IsNullOrEmpty(it.OnlineMod.ModId))
            .Select(it => new ModViewModel(it.OnlineMod, it))
            .OrderBy(it => it.OnlineMod.Title)
            .ToList();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ViewModelService.Resolve<MainViewModel>().MissingDependencyModsCount = AllMods.Count(it => it.IsDependencyMissing);
            AllMods.ForEach(Mods.Add);
        });
        IsLoading = false;
        stopwatch.Stop();
        Log.Information("Loaded {Count} mods in {Cost}ms", Mods.Count, stopwatch.ElapsedMilliseconds);
        _ = Task.WhenAll(Mods.AsParallel().Select(async mod => await mod.LoadCoverAsync()));
        InitLabels();
    }
    
    public void Dispose()
    {
        if (ModLabels is not null) ModLabels.CollectionChanged -= ModLabelsOnCollectionChanged;
        GC.SuppressFinalize(this);
    }

    public override string NavHeader => NavigationService.Main;
    public ObservableCollection<ModViewModel> Mods { get; } = [];
    public ObservableCollection<ItemLabelViewModel>? ModLabels { get; set; }

    public bool IsHiddenLabel => SelectedLabel?.Title == ItemLabelViewModel.Hidden;

    public ObservableCollection<ModViewModel> ExpanderListMods => SelectedLabel?.Items ?? [];

    private void InitLabels()
    {
        List<ItemLabelViewModel>? list = null;
        if (File.Exists(GetFilePath())) list = JsonSerializer
                .Deserialize<List<TitleOnlyViewModel>>(File.ReadAllText(GetFilePath()), TitleOnlyContext.Default.ListTitleOnlyViewModel)?
                .Select(model => new ItemLabelViewModel(model.Title, AllMods, Colors.LightBlue))
                .ToList();
        Dispatcher.UIThread.Invoke(() =>
        {
            ModLabels = list is null || list.Count < 1 ? [new()] : new ObservableCollection<ItemLabelViewModel>(list);
            ModLabels.CollectionChanged += ModLabelsOnCollectionChanged;
            ModLabels.ForEach(vm => vm.PropertyChanged += ModLabelViewModelOnPropertyChanged);
            SelectedLabel = ModLabels[0];
            ModLabels.First(model => model.Title == ItemLabelViewModel.Hidden).Items.ForEach(mod => Mods.Remove(mod));
        });
    }

    private void ModLabelViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SaveLabels();
    }

    private void ModLabelsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SaveLabels();
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

    private static string GetFilePath(string fileName = "list")
    {
        return Path.Combine(Services.ModLabelsPath, fileName + ".json");
    }

    [RelayCommand]
    private void SwitchAllMod() => SelectedLabel!.Items.ForEach(it => it.SwitchModCommand.Execute(null));

    [RelayCommand]
    private void AddToLabel(string label) => SelectedMods.ToArray().ForEach(mod =>
    {
        var mods = ModLabels?.First(it => it.Title == label).Items ?? [];
        if (!mods.Contains(mod)) mods.Add(mod);
        if (label == ItemLabelViewModel.Hidden) Mods.Remove(mod);
    });

    public bool IsSelectedLabel => 
        SelectedLabel is not null && (SelectedInLabelMods.Count > 0 ? SelectedInLabelMods : SelectedMods).All(SelectedLabel.Items.Contains);

    [RelayCommand(CanExecute = nameof(IsSelectedLabel))]
    private void RmFromLabel()
    {
        var selectedMods = SelectedInLabelMods.Count > 0 ? SelectedInLabelMods : SelectedMods;
        var label = SelectedLabel!.Title;
        selectedMods.ToArray().ForEach(mod =>
        {
            ExpanderListMods.Remove(mod);
            if (label == ItemLabelViewModel.Hidden) Mods.Add(mod);
        });
    }
    
    [RelayCommand]
    private void Rename()
    {
        if (SelectedLabel is null) return;
        SelectedLabel.IsEditing = true;
    }

    [RelayCommand]
    private async Task AddModAsync()
    {
        var files = 
            await Services.Dialog.ShowPickupFilesDialogAsync("Select Mods to Add", true);
        files
            .Select(it => it.TryGetLocalPath())
            .Where(s => !string.IsNullOrEmpty(s))
            .Cast<string>()
            .ForEach(SmapiModInstaller.Install);
    }

    [RelayCommand]
    private void RemoveLabel()
    {
        if (SelectedLabel is null) return;
        ModLabels?.Remove(SelectedLabel);
    }

    [RelayCommand]
    private void AddLabel()
    {
        ModLabels?.Add(new ItemLabelViewModel("Test", [], Colors.Aqua));
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