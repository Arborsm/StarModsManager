using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using StarModsManager.Models;

namespace StarModsManager.ViewModels;

public class ModListViewModel : ViewModelBase
{
    private string? _searchText;
    private bool _isBusy;
    private ModViewModel? _selectedAlbum;
    private CancellationTokenSource? _cancellationTokenSource;
    
    public ReactiveCommand<Unit, ModViewModel?> BuyMusicCommand { get; }

    public ModListViewModel()
    {
        this.WhenAnyValue(x => x.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(400)) // 停止键入 400 毫秒后
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(DoSearch!);

        BuyMusicCommand = ReactiveCommand.Create(() => SelectedAlbum);

        RxApp.MainThreadScheduler.Schedule(LoadMods);
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

    private async void DoSearch(string s)
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
    
    public ObservableCollection<ModViewModel> SearchResults { get; } = [];
    public string? SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }
    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }
    public ModViewModel? SelectedAlbum
    {
        get => _selectedAlbum;
        set => this.RaiseAndSetIfChanged(ref _selectedAlbum, value);
    }
}