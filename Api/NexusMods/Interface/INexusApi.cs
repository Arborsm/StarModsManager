using RestEase;
using StarModsManager.Api.NexusMods.Responses;

namespace StarModsManager.Api.NexusMods.Interface;

public interface INexusApi
{
        [Header("apikey")]
        string ApiKey { get; set; }

        [Header("User-Agent")]
        string UserAgent { get; set; }

        #region Mods

        [Get("v1/games/{gameDomainName}/mods/updated.json")]
        Task<List<ModUpdate>> GetUpdatedMods([Path] string gameDomainName, [Query] string period);
        
        // Strange response ???
        //[Get("v1/games/{gameDomainName}/mods/{modId}/changelogs.json")]
        //Task<List<Changelog>> GetModChangelogs([Path] string gameDomainName, [Path] int modId); 

        [Get("v1/games/{gameDomainName}/mods/latest_added.json")]
        Task<List<ModInfo>> GetLatestAddedMods([Path] string gameDomainName);

        [Get("v1/games/{gameDomainName}/mods/latest_updated.json")]
        Task<List<ModInfo>> GetLatestUpdatedMods([Path] string gameDomainName);

        [Get("v1/games/{gameDomainName}/mods/trending.json")]
        Task<List<ModInfo>> GetTrendingMods([Path] string gameDomainName);

        [Get("v1/games/{gameDomainName}/mods/{id}.json")]
        Task<ModInfo> GetMod([Path] string gameDomainName, [Path] int id);

        [Get("v1/games/{gameDomainName}/mods/md5_search/{md5Hash}.json")]
        Task<ModHashResult> SearchModByMd5([Path] string gameDomainName, [Path] string md5Hash);

        [Post("v1/games/{gameDomainName}/mods/{id}/endorse.json")]
        Task EndorseMod([Path] string gameDomainName, [Path] int id);

        [Post("v1/games/{gameDomainName}/mods/{id}/abstain.json")]
        Task AbstainFromEndorsingMod([Path] string gameDomainName, [Path] int id);

        #endregion

        #region ModInfo Files

        [Get("v1/games/{gameDomainName}/mods/{modId}/files.json")]
        Task<ModFileList> GetModFiles([Path] string gameDomainName, [Path] int modId);

        [Get("v1/games/{gameDomainName}/mods/{modId}/files/{fileId}.json")]
        Task<ModFile> GetModFile([Path] string gameDomainName, [Path] int modId, [Path] int fileId);

        [Get("v1/games/{gameDomainName}/mods/{modId}/files/{id}/download_link.json")]
        Task<ModFileDownloadLink> GetModFileDownloadLink([Path] string gameDomainName, [Path] int modId, [Path] int id);

        #endregion

        #region Games

        [Get("v1/games.json")]
        Task<List<Game>> GetGames([Query("include_unapproved")] bool includeUnapproved);

        [Get("v1/games/{gameDomainName}.json")]
        Task<Game> GetGame([Path] string gameDomainName);

        #endregion

        #region User

        [Get("v1/users/validate.json")]
        Task<UserValidation> ValidateUser();

        [Get("v1/user/tracked_mods.json")]
        Task<List<TrackedMod>> GetTrackedMods();

        [Post("v1/user/tracked_mods.json")]
        Task TrackMod([Query("domain_name")] string domainName, [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> formData);

        [Delete("v1/user/tracked_mods.json")]
        Task UntrackMod([Query("domain_name")] string domainName, [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> formData);

        [Get("v1/user/endorsements.json")]
        Task<List<UserEndorsement>> GetUserEndorsements();

        #endregion

        #region Colour Schemes

        [Get("v1/colourschemes.json")]
        Task<List<ColourScheme>> GetColourSchemes();

        #endregion
}