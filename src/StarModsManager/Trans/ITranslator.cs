using System.Text.Json;
using System.Text.Json.Serialization;
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
    ReadCommentHandling = JsonCommentHandling.Skip,
    GenerationMode = JsonSourceGenerationMode.Default
)]
[JsonSerializable(typeof(Dictionary<string, string>))]
public partial class TranslationContext : JsonSerializerContext
{
    internal static Dictionary<string, string> GetMap(string json)
    {
        var formedJson = json.Replace("\t", " ");
        return JsonSerializer.Deserialize(formedJson, Default.DictionaryStringString) ??
               [];
    }
}