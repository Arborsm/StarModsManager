using System.Collections.ObjectModel;
using StarModsManager.Api;
using StarModsManager.Common.Main;
using StarModsManager.Common.Mods;
using StarModsManager.ViewModels.Items;

namespace StarModsManager.ViewModels.Pages;

public class MainPageViewModel : ViewModelBase, IViewModel
{
    public MainPageViewModel()
    {
        Task.Run(LoadMods);
    }

    public ObservableCollection<ModViewModel> Mods { get; } = [];
    
    public async void LoadMods()
    {
        await ModData.Instance.FindModsAsync(Services.MainConfig.DirectoryPath);
        ModData.Instance.LocalModsMap.Values
            .AsParallel()
            .Select(it => new ModViewModel(it.OnlineMod, it))
            .OrderBy(it => it.OnlineMod.Title)
            .Where(it => !string.IsNullOrEmpty(it.OnlineMod.ModId))
            .ForEach(it => Mods.Add(it));

        var tasks = Mods.Select(mod => (Func<TimeSpan, CancellationToken, Task>)
            (async (delay, ct) => await mod.LoadCover(delay, ct)));
        await HttpBatchExecutor.Instance.ExecuteBatchAsync(tasks, cancellationToken: CancellationToken.None);
    }
}