using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using StarModsManager.Models;

namespace StarModsManager.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<ModViewModel> Mods { get; } = [];
    public ICommand BuyMusicCommand { get; }
    public Interaction<ModListViewModel, ModViewModel?> ShowDialog { get; }

    public MainWindowViewModel()
    {
        ShowDialog = new Interaction<ModListViewModel, ModViewModel?>();

        BuyMusicCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var store = new ModListViewModel();
            var result = await ShowDialog.Handle(store);
            if (result != null)
            {
                Mods.Add(result);
                await result.SaveToDiskAsync();
            }
        });
        RxApp.MainThreadScheduler.Schedule(LoadMods);
    }

    private async void LoadMods()
    {
        var mods = (await Mod.LoadCachedAsync()).Select(x => new ModViewModel(x));

        foreach (var mod in mods)
        {
            Mods.Add(mod);
        }

        foreach (var mod in Mods.ToList())
        {
            await mod.LoadCover();
        }
    }
}
