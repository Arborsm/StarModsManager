using Serilog;
using StardewModdingAPI;
using StarModsManager.Assets;

namespace StarModsManager.Api.NexusMods;

public class NexusDownload(string? modUrl = default)
{
    private int _modId = int.TryParse(modUrl?.Split('/').Last(), out var result) ? result : -1;

    public async Task GetModDownloadUrlAsync(ISemanticVersion? version = null)
    {
        try
        {
            if (_modId < 1) return;
            var mods = await NexusManager.GetModFilesAsync(_modId);
            if (mods is null) return;
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
                return;
            }

            Services.PopUp.AddDownload(link);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to Download Mod: {modId}", _modId);
        }
    }

    public static NexusDownload Create(string modId)
    {
        return new NexusDownload { _modId = int.Parse(modId) };
    }
}