using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using StarModsManager.Api;
using StarModsManager.Api.SMAPI;
using StarModsManager.ViewModels.Customs;

namespace StarModsManager.ViewModels.Items;

public partial class DownloadItemViewModel : ViewModelBase, IDisposable
{
    private readonly string _fileUrl;
    private readonly HttpClient _httpClient;
    private readonly Stopwatch _speedTimer = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;
    private long _lastBytesRead;

    public DownloadItemViewModel(string fileUrl)
    {
        _fileUrl = fileUrl;
        _httpClient = new HttpClient();

        FileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
        Status = "Ready to download";
        Progress = 0;
        IsDownloading = false;
        IsCompleted = false;

        StartDownload();
    }

    [ObservableProperty]
    public partial string FileName { get; set; }

    [ObservableProperty]
    public partial double Progress { get; set; }

    [ObservableProperty]
    public partial bool IsDownloading { get; set; }

    [ObservableProperty]
    public partial bool IsCompleted { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; }

    [ObservableProperty]
    private partial string DownloadSpeed { get; set; } = "0 KB/s";

    [ObservableProperty]
    private partial string FileSize { get; set; } = "Unknown";

    [ObservableProperty]
    private partial string DownloadedSize { get; set; } = "0 KB";

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private async void StartDownload()
    {
        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            IsDownloading = true;
            Status = $"{DownloadedSize}/{FileSize}, {DownloadSpeed}";
            _speedTimer.Restart();

            var filePath = Path.Combine(Services.DownloadPath, FileName);
            long resumePosition = 0;

            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                resumePosition = fileInfo.Length;

                var headRequest = new HttpRequestMessage(HttpMethod.Head, _fileUrl);
                using var headResponse = await _httpClient.SendAsync(headRequest);
                headResponse.EnsureSuccessStatusCode();
                var total = headResponse.Content.Headers.ContentLength;
                if (total.HasValue && fileInfo.Length == total.Value)
                {
                    IsCompleted = true;
                    IsDownloading = false;
                    Progress = 100;
                    FileSize = FormatFileSize(total.Value);
                    DownloadedSize = FileSize;
                    Status = "File already exists";
                    return;
                }
            }

            var request = new HttpRequestMessage(HttpMethod.Get, _fileUrl);
            if (resumePosition > 0) request.Headers.Range = new RangeHeaderValue(resumePosition, null);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;
            FileSize = FormatFileSize(totalBytes ?? 0);

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(filePath,
                resumePosition > 0 ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[8192];
            var totalBytesRead = resumePosition;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, _cancellationTokenSource.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), _cancellationTokenSource.Token);
                totalBytesRead += bytesRead;

                if (totalBytes.HasValue) Progress = (double)totalBytesRead / totalBytes.Value * 100;

                UpdateDownloadStats(totalBytesRead);
            }

            IsCompleted = true;
            Status = "Completed";

            IsDownloading = false;
            _speedTimer.Stop();
        }
        catch (OperationCanceledException)
        {
            Status = "Cancelled";
            IsDownloading = false;
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            Log.Error(ex, "Error downloading file: {Url}", _fileUrl);
            IsDownloading = false;
        }
    }

    private void UpdateDownloadStats(long totalBytesRead)
    {
        var elapsed = _speedTimer.Elapsed.TotalSeconds;
        if (elapsed >= 1)
        {
            var bytesPerSecond = (totalBytesRead - _lastBytesRead) / elapsed;
            DownloadSpeed = $"{FormatFileSize((long)bytesPerSecond)}/s";
            DownloadedSize = FormatFileSize(totalBytesRead);
            Status = $"{DownloadedSize}/{FileSize}, {DownloadSpeed}";
            _lastBytesRead = totalBytesRead;
            _speedTimer.Restart();
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        var order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    [RelayCommand]
    private void PauseResume()
    {
        if (IsDownloading)
        {
            _cancellationTokenSource?.Cancel();
            Status = $"{DownloadedSize}/{FileSize}, Paused";
            IsDownloading = false;
        }
        else
        {
            StartDownload();
        }
    }

    [RelayCommand]
    private void Retry()
    {
        if (!IsDownloading)
        {
            Progress = 0;
            IsCompleted = false;
            StartDownload();
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        try
        {
            var filePath = Path.Combine(Services.DownloadPath, FileName);
            if (File.Exists(filePath)) await Task.Run(() => File.Delete(filePath));
            Status = "Deleted";
        }
        catch (Exception ex)
        {
            Status = $"Delete failed: {ex.Message}";
        }

        Dispose();
        ViewModelService.Resolve<DownloadManagerViewModel>().Downloads.Remove(this);
    }

    [RelayCommand]
    private void OpenFile()
    {
        var filePath = Path.Combine(Services.DownloadPath, FileName);
        if (File.Exists(filePath)) PlatformHelper.OpenFileOrUrl(filePath);
    }

    [RelayCommand]
    private void Install()
    {
        var filePath = Path.Combine(Services.DownloadPath, FileName);
        if (File.Exists(filePath)) SmapiModInstaller.Install(filePath);
    }

    [RelayCommand]
    private void OpenDownloadFolder()
    {
        PlatformHelper.OpenFileOrUrl(Services.DownloadPath);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _httpClient.Dispose();
            _speedTimer.Stop();
        }

        _disposed = true;
    }

    ~DownloadItemViewModel()
    {
        Dispose(false);
    }
}