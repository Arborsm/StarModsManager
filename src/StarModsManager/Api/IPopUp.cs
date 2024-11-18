namespace StarModsManager.Api;

public interface IPopUp
{
    void ShowDownloadManager();
    void AddDownload(string url);
    void ShowFlyout(object flyout);
}