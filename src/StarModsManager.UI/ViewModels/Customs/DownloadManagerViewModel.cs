using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using StarModsManager.Api;
using StarModsManager.ViewModels.Items;

namespace StarModsManager.ViewModels.Customs;

public partial class DownloadManagerViewModel : ViewModelBase, IDisposable
{
    private const int MaxConcurrentDownloads = 3;
    private readonly SemaphoreSlim _downloadSemaphore = new(MaxConcurrentDownloads);

    public DownloadManagerViewModel()
    {
        Downloads.CollectionChanged += OnDownloadsOnCollectionChanged;
    }
    
    private void OnDownloadsOnCollectionChanged(object? o, NotifyCollectionChangedEventArgs args) => 
        Dispatcher.UIThread.Invoke(() => ViewModelService.Resolve<MainViewModel>().DownloadItemsCount = Downloads.Count);

    public ObservableCollection<DownloadItemViewModel> Downloads { get; } = [];

    [RelayCommand]
    private async Task NewDownload()
    {
        var url = await Services.Dialog.ShowDownloadDialogAsync();
        if (!string.IsNullOrEmpty(url)) AddDownload(url);
    }

    public void AddDownload(string url) => _ = Task.Run(() => AddDownloadAsync(url));

    private async Task AddDownloadAsync(string url)
    {
        await _downloadSemaphore.WaitAsync();

        try
        {
            var download = new DownloadItemViewModel(url);
            Dispatcher.UIThread.Invoke(() => Downloads.Add(download));

            // 监视下载完成
            var completion = new TaskCompletionSource<bool>();
            PropertyChangedEventHandler? handler = null;
            handler = (_, e) =>
            {
                if (e.PropertyName != nameof(DownloadItemViewModel.IsCompleted) || !download.IsCompleted) return;
                download.PropertyChanged -= handler;
                _downloadSemaphore.Release();
                completion.SetResult(true);
            };
            download.PropertyChanged += handler;

            await completion.Task;
        }
        catch
        {
            _downloadSemaphore.Release();
            throw;
        }
    }
    
    [RelayCommand]
    private void OpenFolder()
    {
        PlatformHelper.OpenFileOrUrl(Services.DownloadPath);
    }

    [RelayCommand]
    private void DeleteAll()
    {
        foreach (var download in Downloads)
        {
            if (download.IsDownloading)
            {
                download.PauseResumeCommand.Execute(null);
            }
            download.DeleteCommand.Execute(null);
        }
        Downloads.Clear();
    }

    public void Dispose()
    {
        Downloads.ForEach(x => x.Dispose());
        Downloads.CollectionChanged -= OnDownloadsOnCollectionChanged;
        GC.SuppressFinalize(this);
    }
}