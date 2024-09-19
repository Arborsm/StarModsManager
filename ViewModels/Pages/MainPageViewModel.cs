using System.Collections.ObjectModel;
using StarModsManager.Api;
using StarModsManager.Common.Main;
using StarModsManager.Common.Mods;

namespace StarModsManager.ViewModels.Pages;

public class MainPageViewModel : ViewModelBase, IViewModel
{
    public MainPageViewModel()
    {
        Task.Run(LoadMods);
    }

    public ObservableCollection<Items.ModViewModel> Mods { get; } = [];

    private async void LoadMods()
    {
        await ModData.Instance.FindModsAsync(Program.MainConfig.DirectoryPath);
        var mods = ModData.Instance.LocalModsMap.Values
            .AsParallel()
            .Select(it => new Items.ModViewModel(it.OnlineMod, it))
            .OrderBy(it => it.OnlineMod.Title);

        foreach (var mod in mods.Where(it => !string.IsNullOrEmpty(it.OnlineMod.ModId))) Mods.Add(mod);

        var tasks = Mods.Select(mod => (Func<TimeSpan, CancellationToken, Task>)(async (delay, ct) =>
            await mod.LoadCover(delay, ct)));

        await HttpBatchExecutor.Instance.ExecuteBatchAsync(tasks, cancellationToken: CancellationToken.None);
    }
}