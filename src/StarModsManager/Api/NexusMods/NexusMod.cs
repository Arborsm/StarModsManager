using HtmlAgilityPack;
using Semver;
using Serilog;

namespace StarModsManager.Api.NexusMods;

public class NexusMod
{
    private const string Author = "//div[h3[text()='Created by']]";
    private const string Description = "//div[@class='container tab-description']//p";
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

    public static async Task<NexusMod> CreateAsync(string modUrl, bool getPics = true,
        CancellationToken cancellationToken = default)
    {
        var url = getPics ? modUrl + "?tab=images" : modUrl;
        var mod = new NexusMod(modUrl)
        {
            Doc = await HttpHelper.Instance.FetchHtmlDocumentAsync(url, cancellationToken)
        };
        return mod;
    }

    public string GetModAuthor()
    {
        var authorNode = Doc.DocumentNode.SelectSingleNode(Author);
        var author = authorNode?.InnerText.Replace("Created by", "").Trim();
        return author ?? string.Empty;
    }

    public string GetModDescription()
    {
        var descriptionNode = Doc.DocumentNode.SelectSingleNode(Description);
        return descriptionNode?.InnerText ?? string.Empty;
    }

    public SemVersion? GetModVersion()
    {
        var versionNode = Doc.DocumentNode.SelectSingleNode(Version);
        if (!int.TryParse(ModUrl.Split('/').Last(), out var result) || result < 1) return null;
        Log.Information("Getting Mod({Result}) Version: {Version}", result, versionNode?.InnerText);
        try
        {
            return versionNode is null ? null : SemVersion.Parse(versionNode.InnerText, SemVersionStyles.Any);
        }
        catch (Exception)
        {
            // ignored
        }

        return null;
    }

    public async Task<string> GetModPicUrlAsync(string picCode)
    {
        var pics = GetModPicsUrl(picCode);
        if (pics is not []) return pics.FirstOrDefault(string.Empty);
        var picHeader = GetModPicsUrl(HeaderPic);
        if (picHeader is not []) return picHeader.FirstOrDefault(string.Empty);
        if (!int.TryParse(ModUrl.Split('/').Last(), out var result) || result < 1) return string.Empty;
        var picsByApi = await NexusManager.GetPicsAsync(result);
        Log.Information("Getting Pic by Mod: {Result}", result);
        return picsByApi?.ToString() ?? string.Empty;
    }

    public string[] GetModPicsUrl(string picCode)
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