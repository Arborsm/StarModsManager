using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarModsManager.Models;

namespace StarModsManager.ViewModels;

public partial class ModListViewModel : ViewModelBase
{
    private readonly Window _window;

    [ObservableProperty]
    private string? _searchText;
    [ObservableProperty]
    private bool _isBusy;
    [ObservableProperty]
    private ModViewModel? _selectedAlbum;
    private CancellationTokenSource? _cancellationTokenSource;
    public ObservableCollection<ModViewModel> SearchResults { get; } = [];

    public ModListViewModel(Window window)
    {
        _window = window;
        Task.Run(LoadMods);
    }

    [RelayCommand]
    private void Close()
    { 
        _window.Close(SelectedAlbum);
    }

    private async void LoadMods()
    {
        IsBusy = true;
        _cancellationTokenSource?.CancelAsync().ConfigureAwait(false);
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;
        
        var mods = await Mod.GetModsAsync();

        SearchResults.Clear();
        foreach (var mod in mods)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            var vm = new ModViewModel(mod);
            SearchResults.Add(vm);
        }

        LoadCovers(cancellationToken);
        
        IsBusy = false;
    }

    [RelayCommand (CanExecute = nameof(CanSearch))]
    private async Task DoSearch(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return;
        IsBusy = true;
        _cancellationTokenSource?.CancelAsync().ConfigureAwait(false);
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;
        
        SearchResults.Clear();
        var mods = await Mod.SearchAsync(s);

        if (s.Equals("test"))
        {
            TestMods(mods, ()=> s.Equals("test"), cancellationToken);
        }
        else
        {
            foreach (var mod in mods)
            {
                var vm = new ModViewModel(mod);
                SearchResults.Add(vm);
            }
            
            if (!cancellationToken.IsCancellationRequested)
            {
                LoadCovers(cancellationToken);
            }
        }
        
        IsBusy = false;
    }

    private bool CanSearch => string.IsNullOrWhiteSpace(SearchText);

    private async void TestMods(IEnumerable<Mod> mods, Func<bool> predicate,
        CancellationToken cancellationToken = default)
    {
        var modList = mods.ToList();
        do
        {
            foreach (var vm in modList.Select(mod => new ModViewModel(mod)))
            {
                SearchResults.Add(vm);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                LoadCovers(cancellationToken);
            }
            await Task.Delay(1000, cancellationToken);
        } while(predicate());
    }
    
    private async void LoadCovers(CancellationToken cancellationToken = default)
    {
        foreach (var mod in SearchResults.ToList())
        {
            await mod.LoadCover();
            
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}