using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using RestEase;
using StarModsManager.Api.NexusMods.Limit;
using StarModsManager.Api.NexusMods.Responses;

namespace StarModsManager.Api.NexusMods.Interface;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    Converters =
    [
        typeof(UnixDateTimeConverter),
        typeof(JsonStringEnumConverter<ModStatus>),
        typeof(JsonStringEnumConverter<EndorsementStatus>),
        typeof(JsonStringEnumConverter<FileCategory>)
    ])]
[JsonSerializable(typeof(ColourScheme))]
[JsonSerializable(typeof(Game))]
[JsonSerializable(typeof(ModFile))]
[JsonSerializable(typeof(ModFileDownloadLink))]
[JsonSerializable(typeof(ModFileDownloadLink[]))]
[JsonSerializable(typeof(ModFileList))]
[JsonSerializable(typeof(DownloadResponse))]
[JsonSerializable(typeof(ModHashResult))]
[JsonSerializable(typeof(ModInfo))]
[JsonSerializable(typeof(ModUpdate))]
[JsonSerializable(typeof(TrackedMod))]
[JsonSerializable(typeof(UserEndorsement))]
[JsonSerializable(typeof(UserValidation))]
[JsonSerializable(typeof(GameCategory))]
[JsonSerializable(typeof(Endorsement))]
[JsonSerializable(typeof(ModFileWithHash))]
[JsonSerializable(typeof(ModFileUpdate))]
[JsonSerializable(typeof(ModStatus))]
[JsonSerializable(typeof(EndorsementStatus))]
[JsonSerializable(typeof(FileCategory))]
internal partial class NexusModsJsonContext : JsonSerializerContext;

internal class SystemTextJsonRequestBodySerializer : RequestBodySerializer
{
    public override HttpContent? SerializeBody<T>(T body, RequestBodySerializerInfo info)
    {
        if (body == null) return null;
        var jsonTypeInfo = (JsonTypeInfo<T>)NexusModsJsonContext.Default.GetTypeInfo(typeof(T))!;
        var json = JsonSerializer.Serialize(body, jsonTypeInfo);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return content;
    }
}

internal class SystemTextJsonResponseDeserializer : ResponseDeserializer
{
    private static T DeserializeJson<T>(string content)
    {
        var jsonTypeInfo = (JsonTypeInfo<T>)NexusModsJsonContext.Default.GetTypeInfo(typeof(T))!;
        var result = JsonSerializer.Deserialize(content, jsonTypeInfo) ??
                     throw new InvalidOperationException("Can't deserialize the response content.");
        return result;
    }

    public override T Deserialize<T>(string? content, HttpResponseMessage response, ResponseDeserializerInfo info)
    {
        if (string.IsNullOrEmpty(content)) throw new InvalidOperationException("The response content is null.");

        try
        {
            RateLimits.DailyLimit = GetHeaderValue("x-rl-daily-limit", int.Parse);
            RateLimits.DailyRemaining = GetHeaderValue("x-rl-daily-remaining", int.Parse);
            RateLimits.DailyReset = GetHeaderValue("x-rl-daily-reset", DateTimeOffset.Parse);
            RateLimits.HourlyLimit = GetHeaderValue("x-rl-hourly-limit", int.Parse);
            RateLimits.HourlyRemaining = GetHeaderValue("x-rl-hourly-remaining", int.Parse);
            RateLimits.HourlyReset = GetHeaderValue("x-rl-hourly-reset", DateTimeOffset.Parse);
        }
        catch (Exception)
        {
            // Ignored
        }

        return DeserializeJson<T>(content);

        T1 GetHeaderValue<T1>(string name, Func<string, T1> parse)
        {
            if (!response.Headers.TryGetValues(name, out var values))
                throw new InvalidOperationException($"The response doesn't include the expected {name} header.");
            var value = values.FirstOrDefault();
            if (value == null)
                throw new InvalidOperationException(
                    $"The response doesn't include the expected {name} header.");
            try
            {
                return parse(value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"The response includes unexpected {name} value: '{value}' can't be converted to {typeof(T1).FullName}.", ex);
            }
        }
    }
}