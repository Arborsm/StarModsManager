namespace StarModsManager.Api.NexusMods;

public class NexusDownload(string modUrl)
{
    public int ModId => int.Parse(modUrl.Split('/').Last());

    public async Task GetModDownloadUrlAsync(CancellationToken cancellationToken = default)
    {
        var mods = await NexusManager.GetModFilesAsync(ModId);
    }
}