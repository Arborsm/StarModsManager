using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using StarModsManager.Api;
using StarModsManager.Api.NexusMods;

namespace StarModsManager.Mods;

public class OnlineMod
{
    private const string DefaultNoImage = "https://www.nexusmods.com/assets/images/default/noimage.svg";
    private readonly string _cachePath;
    private readonly Lazy<Task<NexusMod>?> _nexusMod;

    public OnlineMod() : this("https://www.nexusmods.com/stardewvalley/mods/14070",
        "Fancy Crops and Foraging Retexture", "")
    {
    }

    public OnlineMod(string url, string title, string picUrl, bool getDetail = false)
    {
        var modId = url.Split('/').Last();
        _cachePath = Path.Combine(Services.OnlineModsDir, modId);
        ModId = modId;
        Url = url;
        Title = title;
        PicUrl = picUrl;
        UpdateUrlType = GetUpdateUrlType(url);
        _nexusMod = getDetail ? new(NexusMod.CreateAsync(url, false)) : new((Task<NexusMod>?)null);
    }

    public string UpdateUrlType { get; }
    public string ModId { get; }
    public string Url { get; }
    public string Title { get; }
    public string PicUrl { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public string? Version { get; set; }

    public async Task LoadDetailAsync()
    {
        if (_nexusMod.Value == null) return;
        var nexusMod = await _nexusMod.Value;
        Description = nexusMod.GetModDescription();
        Author = nexusMod.GetModAuthor();
        Version = nexusMod.GetModVersion()?.ToString();
    }

    private static string GetUpdateUrlType(string? url)
    {
        return url?.ToLower() switch
        {
            { } s when s.Contains("nexusmods") => "NexusMods",
            { } s when s.Contains("github") => "Github",
            { } s when s.Contains("playstarbound") => "Forums",
            { } s when s.Contains("stardewvalleywiki") => "Unofficial",
            { } s when s.Contains("spacechase0") => "Spacechase",
            _ => "???"
        };
    }

    public async void SaveAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(ModId)) return;
            await using var fs = File.OpenWrite(_cachePath);
            await SaveToStreamAsync(this, fs);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error in OnlineMod SaveAsync");
        }
    }

    private static async Task SaveToStreamAsync(OnlineMod data, Stream stream)
    {
        await JsonSerializer.SerializeAsync(stream, data, OnlineModContent.Default.OnlineMod);
    }

    public async Task<Stream?> LoadPicBitmapAsync(bool refresh = false, CancellationToken cancellationToken = default)
    {
        if (refresh && File.Exists(_cachePath + ".bmp")) File.Delete(_cachePath + ".bmp");
        if (File.Exists(_cachePath + ".bmp")) return File.OpenRead(_cachePath + ".bmp");

        if (string.IsNullOrEmpty(Url) || Url.Contains("???")) return null;

        if (refresh || string.IsNullOrEmpty(PicUrl) || !PicUrl.Contains("http") || PicUrl == DefaultNoImage)
        {
            var mod = await NexusMod.CreateAsync(Url, true, cancellationToken);
            PicUrl = await mod.GetModPicUrlAsync(NexusMod.Pics);
        }

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

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(OnlineMod))]
internal partial class OnlineModContent : JsonSerializerContext;