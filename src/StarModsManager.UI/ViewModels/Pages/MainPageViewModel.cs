using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text.Json;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StarModsManager.Api;
using StarModsManager.lib;
using StarModsManager.ViewModels.Items;

namespace StarModsManager.ViewModels.Pages;

public partial class MainPageViewModel : MainPageViewModelBase
{
    private readonly ObservableCollection<ModViewModel> _hiddenMods = [];

    [ObservableProperty]
    private bool _isLoading = true;

    private List<ModViewModel> _mods = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpanderListMods))]
    private ItemLabelViewModel? _selectedLabel;

    [ObservableProperty]
    private IList<ModViewModel>? _selectedMods;

    public MainPageViewModel()
    {
        InitLabels();
        WeakReferenceMessenger.Default.Register<ModHiddenChangedMessage>(this, Handler);
    }

    public override string NavHeader => NavigationService.Main;
    public ObservableCollection<ModViewModel> Mods { get; } = [];
    public ObservableCollection<ItemLabelViewModel> ModLabels { get; set; } = null!;

    public bool IsHiddenLabel => SelectedLabel?.Title == "Hidden";

    public ObservableCollection<ModViewModel> ExpanderListMods => IsHiddenLabel
        ? _hiddenMods
        : new(_mods.Where(mod => SelectedLabel?.Items?.Any(it => it == mod.LocalMod!.UniqueID) ?? false));

    private void InitLabels()
    {
        List<ItemLabelViewModel>? list = null;
        if (File.Exists(GetFilePath()))
            list = JsonSerializer
                .Deserialize<List<TitleOnlyViewModel>>(File.ReadAllText(GetFilePath()),
                    TitleOnlyContext.Default.ListTitleOnlyViewModel)?
                .Select(it => new ItemLabelViewModel(it.Title))
                .ToList();
        ModLabels = list is null || list.Count < 1 ? [new()] : new(list);
        ModLabels.CollectionChanged += ModLabelsOnCollectionChanged;
    }

    private void ModLabelsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var list = ModLabels.Select(it => new TitleOnlyViewModel(it)).ToList();
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

    private void Handler(object _, ModHiddenChangedMessage message)
    {
        var mods = message.Hide ? Mods : _hiddenMods;
        var mod = mods.First(it => it.LocalMod!.UniqueID == message.ModId);
        Dispatcher.UIThread.Invoke(() =>
        {
            if (message.Hide)
            {
                _hiddenMods.Add(mod);
                Mods.Remove(mod);
            }
            else
            {
                Mods.Add(mod);
                _hiddenMods.Remove(mod);
            }
        });
    }

    public async Task LoadModsAsync()
    {
        await ModsHelper.Instance.FindModsAsync();
        IsLoading = true;
        Mods.Clear();
        var stopwatch = Stopwatch.StartNew();
        _mods = ModsHelper.Instance.LocalModsMap.Values
            .Where(it => !string.IsNullOrEmpty(it.OnlineMod.ModId))
            .Select(it => new ModViewModel(it.OnlineMod, it))
            .OrderBy(it => it.OnlineMod.Title)
            .ToList();

        var groupedMods = _mods.ToLookup(it => !it.LocalMod!.IsHidden);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            groupedMods[true].ForEach(mod => Mods.Add(mod));
            groupedMods[false].ForEach(mod => _hiddenMods.Add(mod));
        });
        IsLoading = false;
        stopwatch.Stop();
        SMMDebug.Info($"Loaded {Mods.Count} mods in {stopwatch.ElapsedMilliseconds}ms");
        await Task.WhenAll(Mods.AsParallel().Select(async mod => await mod.LoadCoverAsync()));
    }

    [RelayCommand]
    private async Task AddModAsync()
    {
        var msg = new PickupFilesDialogMessage
        {
            Title = "Select Mods to Add",
            AllowMultiple = true
        };
        WeakReferenceMessenger.Default.Send(msg);
        var files = await msg.CompletionSource.Task;
        files
            .Select(it => it.TryGetLocalPath())
            .Where(s => !string.IsNullOrEmpty(s))
            .Cast<string>()
            .ForEach(ModsHelper.Install);
    }

    [RelayCommand]
    private void RemoveLabel()
    {
        if (SelectedLabel is null) return;
        ModLabels.Remove(SelectedLabel);
    }

    [RelayCommand]
    private void AddLabel()
    {
        ModLabels.Add(new ItemLabelViewModel("Test", Colors.Aqua, []));
    }

    public void ReFreshLabels()
    {
        var list = ModLabels.ToList();
        ModLabels.Clear();
        list.Select(label => new ItemLabelViewModel(label.Title, label.BorderColor, label.Items ?? []))
            .ForEach(ModLabels.Add);
    }
}