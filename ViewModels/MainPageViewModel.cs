using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using StarModsManager.Models;
using StarModsManager.Views.Windows;

namespace StarModsManager.ViewModels;

public partial class MainPageViewModel
{
    public ObservableCollection<ModViewModel> Mods { get; } = [];

    public MainPageViewModel()
    {
        Task.Run(LoadMods);
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

    private async Task LoadMods()
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