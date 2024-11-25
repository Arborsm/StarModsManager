namespace StarModsManager.Api;

public interface ILifeCycle
{
    void Reset();
    void ShowDownloadManager();
    void AddDownload(string url);
}