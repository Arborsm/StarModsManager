using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text;
using HtmlAgilityPack;
using StarModsManager.Common.Mods;

namespace StarModsManager.Api.NexusMods;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public class NexusPage
{
    public static readonly Dictionary<string, string> Types = new()
    {
        ["Date"] = "date",
        ["Endorsements"] = "OLD_endorsements",
        ["Downloads"] = "OLD_downloads",
        ["Unique Downloads"] = "OLD_u_downloads",
        ["Latest Updated"] = "lastupdate",
        ["Author"] = "author",
        ["File Name"] = "name",
        ["Size"] = "OLD_size",
        ["Trending"] = "two_weeks_ratings",
        ["Last Comment"] = "lastcomment",
        ["Random"] = "RAND"
    };
    public bool Advfilt { get; set; } = true;
    public int GameId { get; set; } = 1303;
    public bool Home { get; set; } = false;
    public bool IncludeAdult { get; set; } = true;

    public bool Nav { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool ShowGameFilter { get; set; } = false;
    public string SortBy { get; set; } = Types["Date"];
    public int Type { get; set; } = 0;
    public int UserId { get; set; } = 0;
    public bool AscOrder { get; set; }

    public string GetUrl(string? searchText = null)
    {
        var url = new StringBuilder();
        url.Append("https://www.nexusmods.com/Core/Libs/Common/Widgets/");
        url.Append("ModList?RH_ModList=");
        url.Append(($"nav:{Nav}," +
                    $"home:{Home}," +
                    $"type:{Type}," +
                    $"user_id:{UserId}," +
                    $"game_id:{GameId}," +
                    $"advfilt:{Advfilt}," +
                    $"include_adult:{IncludeAdult}," +
                    $"show_game_filter:{ShowGameFilter}," +
                    $"page_size:{PageSize}," +
                    $"page:{Page},").ToLower());
        url.Append(AscOrder ? "order:ASC," : "order:DESC,");
        url.Append($"sort_by={SortBy}");
        if (!string.IsNullOrEmpty(searchText)) url.Append($",search_filename={searchText}");
        var fullUrl = url.ToString();
        StarDebug.Info($"Get Mods Url: {fullUrl}");
        return fullUrl;
    }
    
    public async Task<IEnumerable<OnlineMod>> GetModsAsync(string? searchText = null, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await HttpHelper.Instance.GetAsync(GetUrl(searchText), cancellationToken);
        }
        catch (Exception e)
        {
            StarDebug.Error($"请求失败，异常信息: {e.Message}");

            response = new HttpResponseMessage();
        }

        var responseData = string.Empty;
        if (response.IsSuccessStatusCode)
            responseData = await response.Content.ReadAsStringAsync(cancellationToken);
        else
            StarDebug.Error($"请求失败，状态码: {response.StatusCode}");

        var document = new HtmlDocument();
        document.LoadHtml(responseData);

        var modTitles = document.DocumentNode.SelectNodes("//div[contains(@class, 'mod-tile')]");

        if (modTitles is not null)
            return from mod in modTitles
                let modNameNode = mod.SelectSingleNode(".//p[@class='tile-name']/a")
                let modName = modNameNode?.InnerText.Trim()
                let modLink = modNameNode?.GetAttributeValue("href", string.Empty)!
                let modPicNode = mod.SelectSingleNode(".//div[@class='fore_div_mods']//img[@class='fore']")
                let modPicLink = modPicNode?.GetAttributeValue("src", string.Empty)
                where !string.IsNullOrEmpty(modPicLink)
                select new OnlineMod(modLink, modName!, modPicLink);

        StarDebug.Error("未找到 mod 信息");
        return [];
    }
}