using Octokit;
using Serilog;
using SharpCompress.Archives;
using FileMode = System.IO.FileMode;

namespace StarModsManager.Api.SMAPI;

public class SmapiInstall
{
    private const string InstallExe = "Windows.bat";
    public static readonly SmapiInstall Instance = new();
    private string? _downloadUrl;

    private SmapiInstall()
    {
    }

    public async Task<bool> Install(string filePath)
    {
        var targetDirectory = Path.Combine(Services.TempDir, "SMAPI");
        try
        {
            Directory.CreateDirectory(targetDirectory);
            if (!File.Exists(filePath) && !await Download(filePath, CancellationToken.None)) return false;
            using var archive = ArchiveFactory.Open(filePath);
            archive.ExtractToDirectory(targetDirectory);
            return PlatformHelper.OpenFileOrUrl(Path.Combine(targetDirectory, InstallExe));
        }
        catch (Exception e)
        {
            Log.Error(e, "Fail to install SMAPI");
            return false;
        }
        finally
        {
            Directory.Delete(targetDirectory, true);
        }
    }

    private async Task<bool> Download(string filePath, CancellationToken cancellationToken)
    {
        if (_downloadUrl is null) return false;
        var response = await HttpHelper.Instance.GetAsync(_downloadUrl, cancellationToken);
        await using var file = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream =
            new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, true);
        await file.CopyToAsync(fileStream, cancellationToken);
        return true;
    }

    public async Task<Version?> GetLatestVersion()
    {
        var client = new GitHubClient(new ProductHeaderValue(Services.AppName));
        var releases = await client.Repository.Release.GetAll("Pathoschild", "SMAPI");
        _downloadUrl = releases[0].Url;
        return Version.TryParse(releases[0].TagName, out var version) ? version : null;
    }
}