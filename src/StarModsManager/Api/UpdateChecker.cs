using System.Globalization;
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
    private const string TimestampFileName = "LastUpdateCheck";
    private const int CheckIntervalMinutes = 10;
    private static string GetTimestampFilePath => Path.Combine(Services.AppSavingPath, TimestampFileName);

    public static void CheckUpdate()
    {
        SaveLastCheckTimestamp();
        
        Task.Run(CheckForUpdatesAsync).ContinueWith(t =>
        {
            var (hasUpdate, latestVersion, downloadUrl) = t.Result;
            if (!hasUpdate) return;
            var msg = string.Format(Lang.UpdateAvailableContent, latestVersion);
            var command = new RelayCommand(() => PlatformHelper.OpenFileOrUrl(downloadUrl));
            Services.Notification?.Show(Lang.UpdateAvailable, msg, Severity.Success,
                TimeSpan.FromSeconds(20), null, null, Lang.DownloadManagerLabel, command);
        });
    }

    public static bool ShouldSkipCheck()
    {
        try
        {
            var timestampPath = GetTimestampFilePath;
            if (!File.Exists(timestampPath)) return false;
            var lastCheckTime = DateTime.Parse(File.ReadAllText(timestampPath));
            var timeSinceLastCheck = DateTime.Now - lastCheckTime;
            Log.Information("Skipping Auto update check, " +
                            "last check was {TimeSinceLastCheck} minutes ago", timeSinceLastCheck.TotalMinutes);
            return timeSinceLastCheck.TotalMinutes < CheckIntervalMinutes;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reading last update check timestamp");
            return false;
        }
    }

    private static void SaveLastCheckTimestamp()
    {
        try
        {
            var timestampPath = GetTimestampFilePath;
            File.WriteAllText(timestampPath, DateTime.Now.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving update check timestamp");
        }
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
            if (Version.TryParse(nexus.GetModVersion()?.ToString() ?? string.Empty, out var latest))
            {
                result = (latest > current, latest.ToString(), NexusUrl);
                isSuccess = latest > current;
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error checking for updates on NexusMods");
        }

        if (!isSuccess)
        {
            try
            {
                var github = new GitHubClient(new ProductHeaderValue("StarModsManager"));
                var releases = await github.Repository.Release.GetAll(Owner, Repo);

                if (releases.Count == 0) return result;

                var latestRelease = releases[0];
                var latestVersionString = latestRelease.TagName.TrimStart('v');
                if (Version.TryParse(latestVersionString ?? string.Empty, out var latestVersion))
                {
                    var hasUpdate = latestVersion > current;

                    if (latestRelease.Assets.Count < 1) return result;
                    var downloadUrl = latestRelease.Assets[0]?.BrowserDownloadUrl ?? latestRelease.HtmlUrl;

                    result = (hasUpdate, latestVersion.ToString(), downloadUrl);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking for updates on Github");
            }
        }

        return result;
    }
}