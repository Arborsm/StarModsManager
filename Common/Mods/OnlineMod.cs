using System.Text.Json;
using StarModsManager.Api;
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
        _cachePath = Path.Combine(Services.MainConfig.CachePath, modId);
        ModId = modId;
        Url = url;
        Title = title;
        PicUrl = picUrl;
    }

    public string ModId { get; init; }
    public string? Url { get; init; }
    public string Title { get; init; }
    public string PicUrl { get; set; }

    public async void SaveAsync()
    {
        if (string.IsNullOrEmpty(ModId)) return;
        if (!Directory.Exists(Services.MainConfig.CachePath)) Directory.CreateDirectory(Services.MainConfig.CachePath);

        await using var fs = File.OpenWrite(_cachePath);
        await SaveToStreamAsync(this, fs);
    }

    private static async Task SaveToStreamAsync(OnlineMod data, Stream stream)
    {
        await JsonSerializer.SerializeAsync(stream, data).ConfigureAwait(false);
    }

    public async Task<Stream?> LoadPicBitmapAsync(TimeSpan delay, bool refresh = false,
        CancellationToken cancellationToken = default)
    {
        if (refresh && File.Exists(_cachePath + ".bmp")) File.Delete(_cachePath + ".bmp");
        if (File.Exists(_cachePath + ".bmp")) return File.OpenRead(_cachePath + ".bmp");
        
        if (string.IsNullOrEmpty(PicUrl) || (refresh && !PicUrl.Contains("http")))
            if (Url is not null)
                PicUrl = await ModLinks.Instance.GetModPicUrl(Url, ModLinks.Pics, cancellationToken);

        if (string.IsNullOrEmpty(PicUrl)) return null;
        await Task.Delay(delay, cancellationToken);
        return await LoadPicBitmapAsync(PicUrl, _cachePath + ".bmp", CancellationToken.None);
    }

    public async Task<Stream?> LoadPicBitmapAsync(string picUrl, string cachePath,
        CancellationToken cancellationToken = default)
    {
        if (File.Exists(cachePath)) return File.OpenRead(cachePath);

        var response = await HttpBatchExecutor.Instance.GetAsync(picUrl, cancellationToken);

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

    public static async Task<IEnumerable<OnlineMod>> SearchAsync(string searchTerm, CancellationToken ct = default)
    {
        return await ModLinks.Instance.GetModsAsync(searchTerm, ct);
    }

    public static async Task<IEnumerable<OnlineMod>> GetModsAsync(CancellationToken ct = default)
    {
        return await ModLinks.Instance.GetModsAsync(cancellationToken: ct);
    }
}