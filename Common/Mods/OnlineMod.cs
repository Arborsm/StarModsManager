using System.Text.Json;
using StarModsManager.Api;
using StarModsManager.Api.NexusMods;
using StarModsManager.Common.Main;

namespace StarModsManager.Common.Mods;

public class OnlineMod
{
    private readonly string _cachePath;

    public OnlineMod() : this("https://www.nexusmods.com/stardewvalley/mods/14070",
        "Fancy Crops and Foraging Retexture", "") { }

    public OnlineMod(string? url, string title, string picUrl)
    {
        var modId = url?.Split('/').Last() ?? string.Empty;
        _cachePath = Path.Combine(Services.OnlineModsDir, modId);
        ModId = modId;
        Url = url;
        Title = title;
        PicUrl = picUrl;
        UpdateUrlType = GetUpdateUrlType(url);
    }

    private static string GetUpdateUrlType(string? url) => url?.ToLower() switch
    {
        { } s when s.Contains("nexusmods") => "NexusMods",
        { } s when s.Contains("github") => "Github",
        { } s when s.Contains("playstarbound") => "Forums",
        { } s when s.Contains("stardewvalleywiki") => "Unofficial",
        { } s when s.Contains("spacechase0") => "Spacechase",
        _ => "???"
    };

    public string UpdateUrlType { get; init; }
    public string ModId { get; init; }
    public string? Url { get; init; }
    public string Title { get; init; }
    public string PicUrl { get; set; }

    public async void SaveAsync()
    {
        if (string.IsNullOrEmpty(ModId)) return;
        await using var fs = File.OpenWrite(_cachePath);
        await SaveToStreamAsync(this, fs);
    }

    private static async Task SaveToStreamAsync(OnlineMod data, Stream stream)
    {
        await JsonSerializer.SerializeAsync(stream, data).ConfigureAwait(false);
    }

    public async Task<Stream?> LoadPicBitmapAsync(bool refresh = false,
        CancellationToken cancellationToken = default)
    {
        if (refresh && File.Exists(_cachePath + ".bmp")) File.Delete(_cachePath + ".bmp");
        if (File.Exists(_cachePath + ".bmp")) return File.OpenRead(_cachePath + ".bmp");
        
        if (string.IsNullOrEmpty(PicUrl) || (refresh && !PicUrl.Contains("http")))
            if (Url is not null && !Url.Contains("???"))
                PicUrl = await new NexusPics(Url).GetModPicUrlAsync(NexusPics.Pics, cancellationToken);

        if (string.IsNullOrEmpty(PicUrl)) return null;
        return await LoadPicBitmapAsync(PicUrl, _cachePath + ".bmp", cancellationToken);
    }

    public async Task<Stream?> LoadPicBitmapAsync(string picUrl, string cachePath,
        CancellationToken cancellationToken = default)
    {
        if (File.Exists(cachePath)) return File.OpenRead(cachePath);

        var response = await HttpHelper.Instance.GetAsync(picUrl, cancellationToken);

        if (!response.IsSuccessStatusCode) return null;
        await using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
        {
            await using (var fileStream =
                         new FileStream(cachePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, true))
            {
                await contentStream.CopyToAsync(fileStream, cancellationToken);
            }
        }

        return File.OpenRead(cachePath);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not OnlineMod other) return false;
        return other.ModId == ModId && other.Url == Url && other.Title == Title;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ModId, Url, Title);
    }
}