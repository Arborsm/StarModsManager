using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Semver;
using Serilog;
using StarModsManager.Api.SMAPI.Model;

namespace StarModsManager.Api.SMAPI;

public static class SMAPI
{
    public static async Task<List<ModEntry>?> GetModUpdateData()
    {
        List<ModSearchEntry> searchEntries = [];
        searchEntries.AddRange(ModsHelper.Instance.LocalModsMap.Values.Select(localMod =>
            new ModSearchEntry(localMod.Manifest.UniqueID, localMod.Manifest.Version, localMod.Manifest.UpdateKeys)));

        var (apiVersion, gameVersion, platform) = GetGameDetail();
        var searchData = new ModSearchData(searchEntries, apiVersion, gameVersion, platform, true);

        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Application-Name", Services.AppName);
        client.DefaultRequestHeaders.Add("Application-Version", Services.AppVersion);
        client.DefaultRequestHeaders.Add("User-Agent", $"{Services.AppName}/{Services.AppVersion}");

        var parsedRequest = JsonSerializer.Serialize(searchData, SMAPIContent.Default.ModSearchData);
        var requestPackage = new StringContent(parsedRequest, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://smapi.io/api/v3.0/mods", requestPackage);

        List<ModEntry>? modUpdateData = [];
        if (response.StatusCode == HttpStatusCode.OK)
        {
            // In the name of the Nine Divines, why is JsonSerializer.Deserialize case-sensitive by default???
            var content = await response.Content.ReadAsStringAsync();
            modUpdateData = JsonSerializer.Deserialize(content, SMAPIContent.Default.ListModEntry);

            if (modUpdateData is null || modUpdateData.Count == 0)
            {
                Log.Information("Mod update data was not parsable from smapi.io");
                Log.Information($"Response from smapi.io:\n{content}");
                Log.Information($"Our request to smapi.io:\n{parsedRequest}");
            }
        }
        else
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Log.Information($"Bad status given from smapi.io: {response.StatusCode}");
                if (response.Content is not null)
                {
                    Log.Information($"Response from smapi.io:\n{await response.Content.ReadAsStringAsync()}");
                }
            }
            else if (response.Content is null)
            {
                Log.Information("No response from smapi.io!");
            }
            else
            {
                Log.Warning("Error getting mod update data from smapi.io!");
            }

            Log.Information($"Our request to smapi.io:\n{parsedRequest}");
        }

        client.Dispose();

        if (modUpdateData != null && modUpdateData.Count != 0)
            Log.Information("Got {Count} mod by SMAPI.io",
                modUpdateData.Count(mod => !string.IsNullOrEmpty(mod.Metadata.Main.Version)));
        return modUpdateData;
    }

    private static (string, string, string) GetGameDetail()
    {
        var apiVersion = GetVersion()?.ToString() ?? "4.1.6";
        return (apiVersion, "1.6.13", "Windows");
    }

    private static SemVersion? GetVersion()
    {
        var path = ModsHelper.Instance.GameFolders.FirstOrDefault()
                   ?? Directory.GetParent(Services.MainConfig.DirectoryPath)?.FullName;
        if (path is null) return null;
        path = Path.Combine(path, "StardewModdingAPI.dll");
        if (!File.Exists(path)) return null;
        var smapiAssembly =
            AssemblyName.GetAssemblyName(path);

        if (smapiAssembly.Version is null) return null;

        return SemVersion.TryParse(
            $"{smapiAssembly.Version.Major}.{smapiAssembly.Version.Minor}.{smapiAssembly.Version.Build}",
            out var version)
            ? version
            : null;
    }
}

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true,
    Converters = [typeof(JsonStringEnumConverter<ModEntryMetadata.WikiCompatibilityStatus>)])]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(ModEntry))]
[JsonSerializable(typeof(ModEntryMetadata))]
[JsonSerializable(typeof(ModEntryMetadata.WikiCompatibilityStatus))]
[JsonSerializable(typeof(ModEntryVersion))]
[JsonSerializable(typeof(ModSearchData))]
[JsonSerializable(typeof(ModSearchEntry[]))]
[JsonSerializable(typeof(ModSearchEntry))]
[JsonSerializable(typeof(List<ModEntry>))]
public partial class SMAPIContent : JsonSerializerContext;