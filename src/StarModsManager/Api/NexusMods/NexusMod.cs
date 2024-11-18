using HtmlAgilityPack;
using Serilog;
using StardewModdingAPI;
using StardewModdingAPI.Toolkit;

namespace StarModsManager.Api.NexusMods;

public class NexusMod
{
    private const string Version = "//li[@class='stat-version']//div[@class='stat']";
    private const string HeaderPic = "//div[@class='img-wrapper header-img']/img";

    public const string Pics =
        "//ul[@id='mod_images_list_1']//li[contains(@class, 'image-tile')]//a[@class='mod-image']";

    private NexusMod(string modUrl)
    {
        ModUrl = modUrl;
    }

    private string ModUrl { get; }
    public required HtmlDocument Doc { get; init; }

    public static async Task<NexusMod> CreateAsync(string modUrl, CancellationToken cancellationToken = default)
    {
        var url = modUrl + "?tab=images";
        var mod = new NexusMod(modUrl)
        {
            Doc = await HttpHelper.Instance.FetchHtmlDocumentAsync(url, cancellationToken)
        };
        return mod;
    }

    public ISemanticVersion? GetModVersionAsync()
    {
        var versionNode = Doc.DocumentNode.SelectSingleNode(Version);
        if (!int.TryParse(ModUrl.Split('/').Last(), out var result) || result < 1) return null;
        Log.Information("Getting Mod({Result}) Version: {Version}", result, versionNode?.InnerText);
        try
        {
            return versionNode is null ? null : new SemanticVersion(versionNode.InnerText, true);
        }
        catch (Exception)
        {
            // Ignore
        }
        return null;
    }

    public async Task<string> GetModPicUrlAsync(string picCode)
    {
        var pics = GetModPicsUrlAsync(picCode);
        if (pics is not []) return pics.FirstOrDefault(string.Empty);
        var picHeader = GetModPicsUrlAsync(HeaderPic);
        if (picHeader is not []) return picHeader.FirstOrDefault(string.Empty);
        if (!int.TryParse(ModUrl.Split('/').Last(), out var result) || result < 1) return string.Empty;
        var picsByApi = await NexusManager.GetPicsAsync(result);
        Log.Information("Getting Pic by Mod: {Result}", result);
        return picsByApi?.ToString() ?? string.Empty;
    }

    public string[] GetModPicsUrlAsync(string picCode)
    {
        var attributeName = GetAttributeName(picCode);
        var imgNodes = Doc.DocumentNode.SelectNodes(picCode);
        return imgNodes?.Select(it => it.GetAttributeValue(attributeName, "")).ToArray() ?? [];
    }

    private static string GetAttributeName(string picCode)
    {
        return picCode switch
        {
            Pics => "href",
            HeaderPic => "src",
            _ => string.Empty
        };
    }
}