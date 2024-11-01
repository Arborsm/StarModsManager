﻿using System.Net.Http;
using RestEase;
using StarModsManager.Api.NexusMods.Responses;
using ModFile = StarModsManager.ViewModels.Customs.ModFile;

namespace StarModsManager.Api.NexusMods.Interface;

public class NexusApiClient
{
    private readonly INexusApi _api;
    private readonly string _gameDomainName;

    public NexusApiClient(string apiKey, string userAgent, string? gameDomainName = default)
    {
        _gameDomainName = gameDomainName ?? "stardewvalley";
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.nexusmods.com/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        var restClient = new RestClient(httpClient)
        {
            ResponseDeserializer = new NexusResponseDeserializer()
        };
        _api = restClient.For<INexusApi>();
        _api.ApiKey = apiKey;
        _api.UserAgent = userAgent;
    }

    public Task<List<ModUpdate>> GetUpdatedMods(string period) => _api.GetUpdatedMods(_gameDomainName, period);

    //public Task<List<Changelog>> GetModChangelogs(int modId) => _api.GetModChangelogs(gameDomainName, modId);

    public Task<List<ModInfo>> GetLatestAddedMods() => _api.GetLatestAddedMods(_gameDomainName);

    public Task<List<ModInfo>> GetLatestUpdatedMods() => _api.GetLatestUpdatedMods(_gameDomainName);

    public Task<List<ModInfo>> GetTrendingMods() => _api.GetTrendingMods(_gameDomainName);

    public Task<ModInfo> GetMod(int id) => _api.GetMod(_gameDomainName, id);

    public Task<ModHashResult> SearchModByMd5(string md5Hash) => _api.SearchModByMd5(_gameDomainName, md5Hash);

    public Task EndorseMod(int id) => _api.EndorseMod(_gameDomainName, id);

    public Task AbstainFromEndorsingMod(int id) => _api.AbstainFromEndorsingMod(_gameDomainName, id);

    public Task<ModFileList> GetModFiles(int modId) => _api.GetModFiles(_gameDomainName, modId);

    public Task<ModFile> GetModFile(int modId, int fileId) => _api.GetModFile(_gameDomainName, modId, fileId);

    public Task<ModFileDownloadLink> GetModFileDownloadLink(int modId, int id) => _api.GetModFileDownloadLink(_gameDomainName, modId, id);

    public Task<List<Game>> GetGames(bool includeUnapproved = false) => _api.GetGames(includeUnapproved);

    public Task<Game> GetGame() => _api.GetGame(_gameDomainName);

    public Task<UserValidation> ValidateUser() => _api.ValidateUser();

    public Task<List<TrackedMod>> GetTrackedMods() => _api.GetTrackedMods();

    public Task TrackMod(string domainName, string modId) => _api.TrackMod(domainName, new Dictionary<string, object>{["mod_id"] = modId});

    public Task UntrackMod(string domainName, string modId) => _api.UntrackMod(domainName, new Dictionary<string, object>{["mod_id"] = modId});

    public Task<List<UserEndorsement>> GetEndorsements() => _api.GetUserEndorsements();

    public Task<List<ColourScheme>> GetColourSchemes() => _api.GetColourSchemes();
}