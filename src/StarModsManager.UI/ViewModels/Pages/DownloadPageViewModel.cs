using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarModsManager.Api.NexusMods;
using StarModsManager.Api.NexusMods.Limit;
using StarModsManager.Assets;
using StarModsManager.Lib;
using StarModsManager.ViewModels.Items;

namespace StarModsManager.ViewModels.Pages;

public partial class DownloadPageViewModel : MainPageViewModelBase
{
    private readonly Throttle _throttle = new(1, TimeSpan.FromSeconds(1));
    private CancellationTokenSource? _cts;
    private int _currentPage = 1;

    [ObservableProperty]
    public partial bool Asc { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial List<ModViewModel?> SelectedMods { get; set; } = [];

    [ObservableProperty]
    public partial string? SortBy { get; set; } = NexusPage.Types.Keys.First();

    public override string NavHeader => NavigationService.Download;
    public ObservableCollection<string> SortByList { get; } = [..NexusPage.Types.Keys];
    public ObservableCollection<ModViewModel> SearchResults { get; } = [];

    private string GetSort => NexusPage.Types[SortBy ?? Lang.Date];
    private bool CanLoadMoreMods => SearchResults.Count > 0;

    private NexusPage NexusPage => new()
    {
        Page = _currentPage,
        SortBy = GetSort,
        AscOrder = Asc
    };

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        _ = DebounceSearchAsync(value);
        _currentPage = 1;
    }

    partial void OnSortByChanged(string? oldValue, string? newValue)
    {
        if (oldValue == null) return;
        if (oldValue == newValue) return;
        Init();
    }

    partial void OnAscChanged(bool oldValue, bool newValue)
    {
        if (oldValue == newValue) return;
        Init();
    }

    private void Init()
    {
        _currentPage = 1;
        SearchResults.Clear();
        RunTask(LoadModsAsync(true));
    }

    private async Task DebounceSearchAsync(string searchText)
    {
        _cts?.CancelAsync();
        _cts = new CancellationTokenSource();

        try
        {
            await Task.Delay(500, _cts.Token);
            RunTask(DoSearchAsync(searchText));
        }
        catch (TaskCanceledException)
        {
        }
    }

    private void RunTask(Task task)
    {
        _ = _throttle.Run(async () => await task);
    }

    [RelayCommand]
    private void Refresh()
    {
        RunTask(string.IsNullOrEmpty(SearchText) ? DoSearchAsync(SearchText) : LoadModsAsync(true));
    }

    [RelayCommand(CanExecute = nameof(CanLoadMoreMods))]
    private async Task LoadMoreModsAsync()
    {
        try
        {
            await Task.Run(() => _throttle.Queue(async () =>
            {
                _currentPage++;
                await LoadModsAsync(false);
            }));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task LoadModsAsync(bool clearSearch)
    {
        IsBusy = true;
        _cts?.CancelAsync();
        _cts = new CancellationTokenSource();
        var cancellationToken = _cts.Token;

        var mods = await NexusPage.GetModsAsync(cancellationToken: cancellationToken);

        if (clearSearch) SearchResults.Clear();
        AddSearchResults(mods.Select(it => new ModViewModel(it)), cancellationToken);

        await LoadCoversAsync(cancellationToken);

        IsBusy = false;
    }

    private async Task DoSearchAsync(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return;
        IsBusy = true;
        _cts?.CancelAsync();
        _cts = new CancellationTokenSource();

        SearchResults.Clear();
        var cancellationToken = _cts.Token;
        var mods = await NexusPage.GetModsAsync(s, cancellationToken);
        AddSearchResults(mods.Select(it => new ModViewModel(it)), cancellationToken);
        if (!cancellationToken.IsCancellationRequested) await LoadCoversAsync(cancellationToken);

        IsBusy = false;
    }

    private async Task LoadCoversAsync(CancellationToken cancellationToken = default)
    {
        var tasks = SearchResults.AsParallel()
            .Select(async model => await model.LoadCoverAsync(cancellationToken: cancellationToken));

        await Task.WhenAll(tasks);
    }

    private void AddSearchResults(IEnumerable<ModViewModel> vms, CancellationToken cancellationToken = default)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            foreach (var mod in vms)
            {
                if (SearchResults.Contains(mod) || cancellationToken.IsCancellationRequested) return;
                SearchResults.Add(mod);
            }
        });
    }
}