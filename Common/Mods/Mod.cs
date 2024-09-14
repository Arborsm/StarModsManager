using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace StarModsManager.Common.Mods;

public class Mod
{
    private static readonly HttpClient HttpClient = new();
    private string _cachePath;

    public Mod(string url, string title, string picUrl)
    {
        var modId = url.Split('/').Last();
        _cachePath = $"./Cache/{modId}";
        ModId = modId;
        Url = url;
        Title = title;
        PicUrl = picUrl;
    }

    public string ModId { get; init; }
    public string Url { get; init; }
    public string Title { get; init; }
    public string PicUrl { get; init; }

    public async Task SaveAsync()
    {
        if (!Directory.Exists("./Cache"))
        {
            Directory.CreateDirectory("./Cache");
        }

        await using var fs = File.OpenWrite(_cachePath);
        await SaveToStreamAsync(this, fs);
    }
    
    public Stream SavePicBitmapStream()
    {
        return File.OpenWrite(_cachePath + ".bmp");
    }

    private static async Task SaveToStreamAsync(Mod data, Stream stream)
    {
        await JsonSerializer.SerializeAsync(stream, data).ConfigureAwait(false);
    }
    
    public static async Task<Mod?> LoadCachedAsync(Stream stream)
    {
        return await JsonSerializer.DeserializeAsync<Mod>(stream).ConfigureAwait(false);
    }

    public static async Task<IEnumerable<Mod>> LoadCachedAsync()
    {
        if (!Directory.Exists("./Cache"))
        {
            Directory.CreateDirectory("./Cache");
        }
        
        var results = new List<Mod>();

        foreach (var file in Directory.EnumerateFiles("./Cache"))
        {
            if (!string.IsNullOrWhiteSpace(new DirectoryInfo(file).Extension) && file.EndsWith("bmp")) continue;
            
            await using var fs = File.OpenRead(file);
            var mod = await LoadCachedAsync(fs).ConfigureAwait(false);
            if (mod != null)
            {
                results.Add(mod);
            }
        }
        
        return results;
    }
    
    public async Task<Stream> LoadPicBitmapAsync(bool delete = false)
    {
        if (delete && File.Exists(_cachePath + ".bmp"))
        {
            File.Delete(_cachePath + ".bmp");
        }
        
        if (File.Exists(_cachePath + ".bmp"))
        {
            return File.OpenRead(_cachePath + ".bmp");
        }

        var data = await HttpClient.GetByteArrayAsync(PicUrl);
        await File.OpenWrite(_cachePath + ".bmp").WriteAsync(data);
        return new MemoryStream(data);
    }

    public static async Task<IEnumerable<Mod>> SearchAsync(string searchTerm)
    {
        return await ModsHelper.Create().GetModsAsync(searchTerm);
    }

    public static async Task<IEnumerable<Mod>> GetModsAsync()
    {
        return await ModsHelper.Create().GetModsAsync();
    }
}