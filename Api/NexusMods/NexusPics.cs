namespace StarModsManager.Api.NexusMods;

public class NexusPics(string modUrl)
{
    private const string HeaderPic = "//div[@class='img-wrapper header-img']/img";
    public const string Pics =
        "//ul[@id='mod_images_list_1']//li[contains(@class, 'image-tile')]//a[@class='mod-image']";

    public async Task<string> GetModPicUrlAsync(string picCode, CancellationToken cancellationToken = default)
    {
        var pics = await GetModPicsUrlAsync(picCode, cancellationToken);
        if (pics is not []) return pics.FirstOrDefault(string.Empty);
        var picHeader = await GetModPicsUrlAsync(HeaderPic, cancellationToken);
        return picHeader.FirstOrDefault(string.Empty);
    }

    public async Task<string[]> GetModPicsUrlAsync(string picCode, CancellationToken cancellationToken = default)
    {
        var url = picCode == Pics ? modUrl + "?tab=images" : modUrl;
        if (string.IsNullOrEmpty(modUrl)) return [];
        try
        {
            var htmlDoc = await HttpHelper.Instance.FetchHtmlDocumentAsync(url, cancellationToken);
            var attributeName = GetAttributeName(picCode);
            var imgNodes = htmlDoc.DocumentNode.SelectNodes(picCode);
            return imgNodes?.Select(it => it.GetAttributeValue(attributeName, "")).ToArray() ?? [];
        }
        catch (Exception ex)
        {
            StarDebug.Error($"Error in GetModPicsUrl: {ex.Message}");
            StarDebug.Error($"Stack Trace: {ex.StackTrace}");
            return [];
        }
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

