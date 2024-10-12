using System.Net.Http;
using HtmlAgilityPack;
using StarModsManager.Api;
using StarModsManager.Common.Main;

namespace StarModsManager.Common.Mods;

public class ModLinks
{
    public const string Header = "//div[@class='img-wrapper header-img']/img";

    public const string Pics =
        "//ul[@id='mod_images_list_1']//li[contains(@class, 'image-tile')]//a[@class='mod-image']";

    private static readonly Lazy<ModLinks> LazyInstance = new(Create);

    private bool _advfilt;
    private int _gameId;
    private bool _home;
    private bool _includeAdult;

    private bool _nav;
    private int _page;
    private int _pageSize;
    private bool _showGameFilter;
    private string _sortBy = null!;
    private int _type;
    private int _userId;

    public static ModLinks Instance => LazyInstance.Value;

    private async Task<HttpResponseMessage> GetModsPage(string? searchText = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // const string url = "https://www.nexusmods.com/stardewvalley/mods";
            var url = "https://www.nexusmods.com/Core/Libs/Common/Widgets/" +
                      "ModList?RH_ModList=" +
                      $"nav:{_nav}," +
                      $"home:{_home}," +
                      $"type:{_type}," +
                      $"user_id:{_userId}," +
                      $"game_id:{_gameId}," +
                      $"advfilt:{_advfilt}," +
                      $"include_adult:{_includeAdult}," +
                      $"show_game_filter:{_showGameFilter}," +
                      $"page_size:{_pageSize}," +
                      $"page:{_page}," +
                      $"sort_by={_sortBy}";
            if (!string.IsNullOrEmpty(searchText)) url += $",search_filename={searchText}";

            return await HttpBatchExecutor.Instance.GetAsync(url, cancellationToken);
        }
        catch (Exception e)
        {
            StarDebug.Error($"请求失败，异常信息: {e.Message}");

            return new HttpResponseMessage();
        }
    }

    public async Task<IEnumerable<OnlineMod>> GetModsAsync(string? searchText = null,
        CancellationToken cancellationToken = default)
    {
        var response = await GetModsPage(searchText, cancellationToken);

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

    public async Task<string> GetModPicUrl(string modUrl, string picCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(modUrl)) return string.Empty;
        try
        {
            var htmlDoc = await FetchHtmlDocument(modUrl, picCode, cancellationToken);
            var attributeName = GetAttributeName(picCode);

            // 查找所需的img标签
            var imgNode = htmlDoc.DocumentNode.SelectSingleNode(picCode);
            return imgNode?.GetAttributeValue(attributeName, "") ?? string.Empty;
        }
        catch (Exception ex)
        {
            StarDebug.Error($"Error in GetModPicUrl: {ex.Message}");
            StarDebug.Error($"Stack Trace: {ex.StackTrace}");
            return string.Empty;
        }
    }

    public async Task<string[]> GetModPicsUrl(string modUrl, string picCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(modUrl)) return [];
        try
        {
            var htmlDoc = await FetchHtmlDocument(modUrl, picCode, cancellationToken);
            var attributeName = GetAttributeName(picCode);

            // 查找所需的img标签
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

    private static async Task<HtmlDocument> FetchHtmlDocument(string modUrl, string picCode,
        CancellationToken cancellationToken)
    {
        if (picCode == Pics) modUrl += "?tab=images";

        var response = await HttpBatchExecutor.Instance.GetAsync(modUrl, cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);
        return htmlDoc;
    }

    private static string GetAttributeName(string picCode)
    {
        return picCode switch
        {
            Pics => "href",
            Header => "src",
            _ => string.Empty
        };
    }

    public static class SortType
    {
        public const string Date = "date";
    }

    #region Builder

    public static ModLinks Create()
    {
        var links = new ModLinks
        {
            _nav = true,
            _home = false,
            _type = 0,
            _userId = 0,
            _gameId = 1303,
            _advfilt = true,
            _includeAdult = false,
            _showGameFilter = false,
            _pageSize = 20,
            _page = 1,
            _sortBy = SortType.Date
        };
        return links;
    }

    public ModLinks SetNav(bool nav)
    {
        _nav = nav;
        return this;
    }

    public ModLinks SetHome(bool home)
    {
        _home = home;
        return this;
    }

    public ModLinks SetType(int type)
    {
        _type = type;
        return this;
    }

    public ModLinks SetGameId(int gameId)
    {
        _gameId = gameId;
        return this;
    }

    public ModLinks SetUserId(int userId)
    {
        _userId = userId;
        return this;
    }

    public ModLinks SetAdvfilt(bool advfilt)
    {
        _advfilt = advfilt;
        return this;
    }

    public ModLinks SetIncludeAdult(bool includeAdult)
    {
        _includeAdult = includeAdult;
        return this;
    }

    public ModLinks SetShowGameFilter(bool showGameFilter)
    {
        _showGameFilter = showGameFilter;
        return this;
    }

    public ModLinks SetPageSize(int pageSize)
    {
        _pageSize = pageSize;
        return this;
    }

    public ModLinks SetPage(int page)
    {
        _page = page;
        return this;
    }

    public ModLinks SetSortBy(string sortBy)
    {
        _sortBy = sortBy;
        return this;
    }

    #endregion
}