using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace StarModsManager.Models;

public class ModsHelper
{
    private bool _nav;
    private bool _home;
    private int _type;
    private int _userId;
    private int _gameId;
    private bool _advfilt;
    private bool _includeAdult;
    private bool _showGameFilter;
    private int _pageSize;
    private int _page;
    private string _sortBy = null!;
    
    public static class SortType
    {
        public const string Date = "date";
    }
    
    private async Task<HttpResponseMessage> GetModsPage(string? searchText = null)
    {
        try
        {
            // const string url = "https://www.nexusmods.com/stardewvalley/mods";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36 Edg/128.0.0.0");
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            client.DefaultRequestHeaders.Add("Referer", "https://www.nexusmods.com/stardewvalley/mods/?BH=3");

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
            if (!string.IsNullOrEmpty(searchText))
            {
                url += $",search_filename={searchText}";
            }
            return await client.GetAsync(url);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    public async Task<IEnumerable<Mod>> GetModsAsync(string? searchText = null)
    {
        var response = await GetModsPage(searchText);

        var responseData = string.Empty;
        if (response.IsSuccessStatusCode)
        {
            responseData = await response.Content.ReadAsStringAsync();
        }
        else
        {
            Console.WriteLine($"请求失败，状态码: {response.StatusCode}");
        }
        
        var document = new HtmlDocument();
        document.LoadHtml(responseData);

        var modTitles = document.DocumentNode.SelectNodes("//div[contains(@class, 'mod-tile')]");

        if (modTitles != null)
        {
            return from mod in modTitles
                let modNameNode = mod.SelectSingleNode(".//p[@class='tile-name']/a")
                let modName = modNameNode?.InnerText.Trim()
                let modLink = modNameNode?.GetAttributeValue("href", string.Empty)!
                let modPicNode = mod.SelectSingleNode(".//div[@class='fore_div_mods']//img[@class='fore']")
                let modPicLink = modPicNode?.GetAttributeValue("src", string.Empty)
                where !string.IsNullOrEmpty(modPicLink)
                select new Mod(modLink, modName!, modPicLink);
        }

        Console.WriteLine("未找到 mod 信息");
        return [];
    }
    
    #region Builder

    public static ModsHelper Create()
    {
        return new ModsHelper
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
    }
    
    public ModsHelper SetNav(bool nav)
    {
        _nav = nav;
        return this;
    }
    
    public ModsHelper SetHome(bool home)
    {
        _home = home;
        return this;
    }
    
    public ModsHelper SetType(int type)
    {
        _type = type;
        return this;
    }
    
    public ModsHelper SetGameId(int gameId)
    {
        _gameId = gameId;
        return this;
    }
    
    public ModsHelper SetUserId(int userId)
    {
        _userId = userId;
        return this;
    }
    
    public ModsHelper SetAdvfilt(bool advfilt)
    {
        _advfilt = advfilt;
        return this;
    }
    
    public ModsHelper SetIncludeAdult(bool includeAdult)
    {
        _includeAdult = includeAdult;
        return this;
    }
    
    public ModsHelper SetShowGameFilter(bool showGameFilter)
    {
        _showGameFilter = showGameFilter;
        return this;
    }
    
    public ModsHelper SetPageSize(int pageSize)
    {
        _pageSize = pageSize;
        return this;
    }
    
    public ModsHelper SetPage(int page)
    {
        _page = page;
        return this;
    }
    
    public ModsHelper SetSortBy(string sortBy)
    {
        _sortBy = sortBy;
        return this;
    }

    #endregion
}