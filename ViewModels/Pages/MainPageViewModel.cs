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
using StarModsManager.Api.lib;
using StarModsManager.Common.Main;
using StarModsManager.ViewModels.Items;

namespace StarModsManager.ViewModels.Pages;

public partial class MainPageViewModel : MainPageViewModelBase
{
    [ObservableProperty]
    private IList<ModViewModel>? _selectedMods;

    private List<ModViewModel> _mods = [];
    private readonly ObservableCollection<ModViewModel> _hiddenMods = [];
    public ObservableCollection<ModViewModel> Mods { get; } = [];
    public ObservableCollection<ItemLabelViewModel> ModCategories { get; }

    public ObservableCollection<ModViewModel> ExpanderListMods => SelectedCategory?.Title == "Hidden"
        ? _hiddenMods
        : new(_mods.Where(mod => SelectedCategory?.Items.Any(it => it == mod.LocalMod!.UniqueID) ?? false));

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpanderListMods))]
    private ItemLabelViewModel? _selectedCategory;

    public MainPageViewModel()
    {
        var list = GetFilePath("List");
        ModCategories = !File.Exists(list) ?  [new ItemLabelViewModel()] :
            new(JsonSerializer.Deserialize<List<ItemLabelViewModel>>(File.ReadAllText(list))!);
        ModCategories.CollectionChanged += ModCategoriesOnCollectionChanged;
        WeakReferenceMessenger.Default.Register<ModHiddenChangedMessage>(this, Handler);
    }

    private static string GetFilePath(string fileName)
    {
        return Path.Combine(Services.ModCategoriesPath, fileName + ".json");
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

    private async void ModCategoriesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var json = JsonSerializer.Serialize(ModCategories.ToList());
        var saveJson = string.Empty;
        var filePath = GetFilePath("List");
        if (File.Exists(filePath)) saveJson = await File.ReadAllTextAsync(filePath);
        if (saveJson != json) await File.WriteAllTextAsync(filePath, json);
    }

    public async Task LoadModsAsync()
    {
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
        if (SelectedCategory is null) return;
        ModCategories.Remove(SelectedCategory);
    }
    
    [RelayCommand]
    private void AddLabel()
    {
        ModCategories.Add(new ItemLabelViewModel("Test", Colors.Aqua));
    }
}