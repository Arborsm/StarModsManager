namespace StarModsManager.Api.NexusMods;

public class NexusDownload(string modUrl)
{
    private int ModId => int.TryParse(modUrl.Split('/').Last(), out var result) ? result : -1;

    public async Task GetModDownloadUrlAsync(CancellationToken cancellationToken = default)
    {
        if (ModId < 1) return;
        var mods = await NexusManager.GetModFilesAsync(ModId);
    }
}