using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using StarModsManager.Api;
using StarModsManager.Common.Mods;
using StarModsManager.ViewModels.Items;
using StarDebug = StarModsManager.Api.StarDebug;

namespace StarModsManager.ViewModels.Pages;

public partial class MainPageViewModel : ViewModelBase 
{
    public ObservableCollection<ModViewModel> Mods { get; } = [];
    
    [ObservableProperty]
    private bool _isLoading = true;

    public async Task LoadModsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var mods = ModData.Instance.LocalModsMap.Values
            .AsParallel()
            .Select(it => new ModViewModel(it.OnlineMod, it))
            .OrderBy(it => it.OnlineMod.Title)
            .Where(it => !string.IsNullOrEmpty(it.OnlineMod.ModId));
        await Dispatcher.UIThread.InvokeAsync(() => mods.ForEach(Mods.Add));
        IsLoading = false;
        stopwatch.Stop();
        StarDebug.Info($"Loaded {Mods.Count} mods in {stopwatch.ElapsedMilliseconds}ms");
        await Task.WhenAll(Mods.AsParallel().Select(async mod => await mod.LoadCoverAsync()));
    }
}