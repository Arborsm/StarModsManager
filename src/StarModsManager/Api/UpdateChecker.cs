using CommunityToolkit.Mvvm.Input;
using Octokit;
using Serilog;
using StarModsManager.Api.NexusMods;
using StarModsManager.Assets;

namespace StarModsManager.Api;

public static class UpdateChecker
{
    private const string Owner = "Arborsm";
    private const string Repo = "StarModsManager";
    private const string NexusUrl = "https://www.nexusmods.com/stardewvalley/mods/29713";

    public static void CheckUpdate()
    {
        Task.Run(CheckForUpdatesAsync).ContinueWith(t =>
        {
            var (hasUpdate, latestVersion, downloadUrl) = t.Result;
            if (!hasUpdate) return;
            var msg = string.Format(Lang.UpdateAvailableContent, latestVersion);
            var command = new RelayCommand(() => PlatformHelper.OpenFileOrUrl(downloadUrl));
            Services.Notification.Show(Lang.UpdateAvailable, msg, Severity.Success,
                TimeSpan.FromSeconds(20), null, null, Lang.DownloadManagerLabel, command);
        });
    }

    private static async Task<(bool hasUpdate, string latestVersion, string downloadUrl)> CheckForUpdatesAsync()
    {
        var currentVersion = Services.AppVersion;
        var current = new Version(currentVersion);
        var result = (false, currentVersion, string.Empty);
        var isSuccess = false;

        try
        {
            var nexus = await NexusMod.CreateAsync(NexusUrl);
            var latest = new Version(nexus.GetModVersion()?.ToString() ?? string.Empty);
            result = (latest > current, latest.ToString(), NexusUrl);
            isSuccess = latest > current;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error checking for updates on NexusMods");
        }

        if (!isSuccess)
            try
            {
                var github = new GitHubClient(new ProductHeaderValue("StarModsManager"));
                var releases = await github.Repository.Release.GetAll(Owner, Repo);

                if (releases.Count == 0) return result;

                var latestRelease = releases[0];
                var latestVersion = latestRelease.TagName.TrimStart('v');
                var latest = new Version(latestVersion);

                var hasUpdate = latest > current;

                if (latestRelease.Assets.Count < 1) return result;
                var downloadUrl = latestRelease.Assets[0]?.BrowserDownloadUrl ?? latestRelease.HtmlUrl;

                result = (hasUpdate, latestVersion, downloadUrl);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking for updates on Github");
            }

        return result;
    }
}