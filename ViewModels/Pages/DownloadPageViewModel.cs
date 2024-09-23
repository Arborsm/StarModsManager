using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarModsManager.Api;
using StarModsManager.Common.Mods;
using StarModsManager.ViewModels.Items;

namespace StarModsManager.ViewModels.Pages;

public partial class DownloadPageViewModel : ViewModelBase, IViewModel
{
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ModViewModel? _selectedAlbum;

    public DownloadPageViewModel()
    {
        Task.Run(LoadMods);
    }

    public ObservableCollection<ModViewModel> SearchResults { get; } = [];

    partial void OnSearchTextChanged(string value)
    {
        _ = DebounceSearchAsync(value);
    }

    private async Task DebounceSearchAsync(string searchText)
    {
        _cts?.CancelAsync().ConfigureAwait(false);
        _cts = new CancellationTokenSource();

        try
        {
            await Task.Delay(500, _cts.Token); // 500ms delay for input
            await DoSearch(searchText);
        }
        catch (TaskCanceledException)
        {
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        if (string.IsNullOrEmpty(SearchText))
            await DoSearch(SearchText);
        else
            await LoadMods();
    }

    private async Task LoadMods()
    {
        IsBusy = true;
        _cts?.CancelAsync().ConfigureAwait(false);
        _cts = new CancellationTokenSource();
        var cancellationToken = _cts.Token;

        var mods = await OnlineMod.GetModsAsync(cancellationToken);

        SearchResults.Clear();
        foreach (var mod in mods)
        {
            if (cancellationToken.IsCancellationRequested) return;
            var vm = new ModViewModel(mod);
            SearchResults.Add(vm);
        }

        LoadCovers(cancellationToken);

        IsBusy = false;
    }

    private async Task DoSearch(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return;
        IsBusy = true;
        _cts?.CancelAsync().ConfigureAwait(false);
        _cts = new CancellationTokenSource();
        var cancellationToken = _cts.Token;

        SearchResults.Clear();
        var mods = await OnlineMod.SearchAsync(s, cancellationToken);

        if (s.Equals("test"))
        {
            TestMods(mods, () => s.Equals("test"), cancellationToken);
        }
        else
        {
            foreach (var mod in mods)
            {
                var vm = new ModViewModel(mod);
                SearchResults.Add(vm);
            }

            if (!cancellationToken.IsCancellationRequested) LoadCovers(cancellationToken);
        }

        IsBusy = false;
    }

    private async void TestMods(IEnumerable<OnlineMod> mods, Func<bool> predicate,
        CancellationToken cancellationToken = default)
    {
        var modList = mods.ToList();
        do
        {
            foreach (var vm in modList.Select(mod => new ModViewModel(mod))) SearchResults.Add(vm);

            if (!cancellationToken.IsCancellationRequested) LoadCovers(cancellationToken);
            await Task.Delay(1000, cancellationToken);
        } while (predicate());
    }

    private async void LoadCovers(CancellationToken cancellationToken = default)
    {
        var tasks = SearchResults.Select(uri =>
            (Func<TimeSpan, CancellationToken, Task>)(async (delay, ct) => await uri.LoadCover(delay, ct)));

        await HttpBatchExecutor.Instance.ExecuteBatchAsync(tasks, cancellationToken: cancellationToken);
    }
}