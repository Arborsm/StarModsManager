using System.Collections.ObjectModel;
using System.Diagnostics;
using StarModsManager.Api;
using StarModsManager.Common.Main;
using StarModsManager.Common.Mods;
using StarModsManager.ViewModels.Items;

namespace StarModsManager.ViewModels.Pages;

public class MainPageViewModel : ViewModelBase
{
    public MainPageViewModel()
    {
        Task.Run(LoadMods);
    }

    public ObservableCollection<ModViewModel> Mods { get; } = [];

    private async void LoadMods()
    {
        var stopwatch = Stopwatch.StartNew();
        await ModData.Instance.FindModsAsync();
        ModData.Instance.LocalModsMap.Values
            .AsParallel()
            .Select(it => new ModViewModel(it.OnlineMod, it))
            .OrderBy(it => it.OnlineMod.Title)
            .Where(it => !string.IsNullOrEmpty(it.OnlineMod.ModId))
            .ForEach(it => Mods.Add(it));
        StarDebug.Info("Loaded {0} mods in {1}ms", Mods.Count, stopwatch.ElapsedMilliseconds);

        var tasks = Mods.Select(mod => (Func<TimeSpan, CancellationToken, Task>)
            (async (delay, ct) => await mod.LoadCover(delay, ct)));
        await HttpBatchExecutor.Instance.ExecuteBatchAsync(tasks, cancellationToken: CancellationToken.None);
    }
}