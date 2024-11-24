using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using TransApiConfig = StarModsManager.Config.TransApiConfig;

namespace StarModsManager.Trans;

public interface ITranslator
{
    bool NeedApi { get; }
    string Name { get; }

    Task<string> StreamCallWithMessageAsync(string text, string role, TransApiConfig config,
        CancellationToken cancellationToken);

    Task<List<string>> GetSupportModelsAsync(TransApiConfig config);
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true,
    AllowTrailingCommas = true, 
    UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
    ReadCommentHandling = JsonCommentHandling.Skip,
    GenerationMode = JsonSourceGenerationMode.Default
)]
[JsonSerializable(typeof(Dictionary<string, string>))]
public partial class TranslationContext : JsonSerializerContext
{
    public static Dictionary<string, string> GetMap(string json)
    {
        var formedJson = json.Replace("\t", " ");
        return JsonSerializer.Deserialize(formedJson, Default.DictionaryStringString) ?? [];
    }

    public static string GetJson(Dictionary<string, string> map) => 
        UnescapeUnicodeChinese(JsonSerializer.Serialize(map, Default.DictionaryStringString));

    private static string UnescapeUnicodeChinese(string json) => UnicodeCjkRegex().Replace(json, match =>
    {
        var unicodeValue = int.Parse(match.Groups[1].Value, NumberStyles.HexNumber);
        if (unicodeValue >= UnicodeRanges.CjkUnifiedIdeographs.FirstCodePoint && 
            unicodeValue <= UnicodeRanges.CjkUnifiedIdeographs.FirstCodePoint + UnicodeRanges.CjkUnifiedIdeographs.Length)
        {
            return char.ConvertFromUtf32(unicodeValue);
        }
        return match.Value;
    });
    
    [GeneratedRegex(@"\\u([0-9a-fA-F]{4})")]
    private static partial Regex UnicodeCjkRegex();
}