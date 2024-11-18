using Serilog;
using StardewModdingAPI;

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
                Services.Notification.Show("Warning", "需要Nexus会员才能直接下载模组，请使用网页一键跳转并下载", Severity.Warning);
            }
            if (link is null)
            {
                Services.Notification.Show("Warning", "未找到/无法下载符合的Mod文件", Severity.Warning);
                return;
            }
            Services.PopUp.AddDownload(link);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to Download Mod: {modId}", _modId);
        }
    }

    public static NexusDownload Create(string modId) => new() { _modId = int.Parse(modId) };
}