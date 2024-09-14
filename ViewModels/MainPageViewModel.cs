using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using StarModsManager.Common.Mods;
using StarModsManager.Views.Windows;

namespace StarModsManager.ViewModels;

public partial class MainPageViewModel
{
    public ObservableCollection<ModViewModel> Mods { get; } = [];

    public MainPageViewModel()
    {
        Dispatcher.UIThread.Post(LoadMods);
    }

    [RelayCommand]
    private async Task BuyMusic()
    {
        var modListWindow = new ModListWindow();
        modListWindow.DataContext = new ModListViewModel(modListWindow);
        var mod = await modListWindow.ShowDialog<ModViewModel?>(MainWindow.Instance);
        if (mod != null && !Mods.Contains(mod))
        {
            Mods.Add(mod);
        }
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