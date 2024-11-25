using Serilog;
using StardewModdingAPI;
using StarModsManager.Assets;

namespace StarModsManager.Api.NexusMods;

public class NexusDownload(string? modUrl = default)
{
    private int _modId = int.TryParse(modUrl?.Split('/').Last(), out var result) ? result : -1;

    public async Task<string?> GetModDownloadUrlAsync(ISemanticVersion? version = null)
    {
        try
        {
            if (_modId < 1) return null;
            var mods = await NexusManager.GetModFilesAsync(_modId);
            if (mods is null) return null;
            var fileId = version is null
                ? mods.Files.First(x => x.Version == mods.Files.Max(file => file.Version)).FileId
                : mods.Files.First(x => x.Version == version.ToString()).FileId;
            var link = (await NexusManager.GetModFileAsync(fileId))?.ToString();
            try
            {
                link ??= (await NexusManager.GetModFileDownloadLinkAsync(_modId, fileId))?[0].Uri;
            }
            catch (NotPremiumException)
            {
                Services.Notification.Show(Lang.Warning, Lang.NexusPremiumRequired, Severity.Warning);
            }

            if (link is null)
            {
                Services.Notification.Show(Lang.Warning, Lang.ModFileNotFound, Severity.Warning);
            }

            return link;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to Download Mod: {modId}", _modId);
            return null;
        }
    }

    public static async Task DownloadLatestModAsync(string modId, ISemanticVersion? version = null)
    {
        var link = await Create(modId).GetModDownloadUrlAsync(version);
        if (link is null) return;
        Services.LifeCycle.AddDownload(link);
    }

    public static NexusDownload Create(string modId)
    {
        return new NexusDownload { _modId = int.Parse(modId) };
    }
}