using System.Text.Json;
using System.Text.Json.Serialization;
using Semver;

namespace StarModsManager.Api.SMAPI;

public class Manifest
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public SemVersion Version { get; set; } = SemVersion.Parse("1.0.0");

    public SemVersion? MinimumApiVersion { get; set; }

    public SemVersion? MinimumGameVersion { get; set; }

    public string? EntryDll { get; set; }

    public ManifestContentPackFor? ContentPackFor { get; set; }

    [JsonConverter(typeof(ManifestDependencyArrayConverter))]
    public ManifestDependency[] Dependencies { get; set; } = [];

    [JsonConverter(typeof(UpdateKeysConverter))]
    public string[] UpdateKeys { get; set; } = [];

    public string UniqueID { get; set; } = string.Empty;

    [JsonExtensionData]
    public IDictionary<string, object> ExtraFields { get; set; } = new Dictionary<string, object>();
}

public class ManifestContentPackFor
{
    public string UniqueID { get; set; } = string.Empty;

    public SemVersion? MinimumVersion { get; set; }
}

public class ManifestDependency
{
    public ManifestDependency(string uniqueID, string? minimumVersion, bool isRequired)
    {
        UniqueID = uniqueID;
        MinimumVersion = minimumVersion is null ? null : SemVersion.Parse(minimumVersion, SemVersionStyles.Any);
        IsRequired = isRequired;
    }

    public ManifestDependency()
    {
    }

    public string UniqueID { get; set; } = string.Empty;

    public SemVersion? MinimumVersion { get; set; }

    public bool IsRequired { get; set; }
}

internal class UpdateKeysConverter : JsonConverter<string[]>
{
    public override string[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array for UpdateKeys.");

        var updateKeys = new List<string>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) break;

            if (reader.TokenType == JsonTokenType.Number)
                updateKeys.Add(reader.GetInt32().ToString());
            else if (reader.TokenType == JsonTokenType.String)
                updateKeys.Add(reader.GetString() ?? string.Empty);
            else
                throw new JsonException($"Unexpected token type in UpdateKeys: {reader.TokenType}");
        }

        return updateKeys.ToArray();
    }

    public override void Write(Utf8JsonWriter writer, string[] value, JsonSerializerOptions options)
    {
        throw new InvalidOperationException("This converter does not write JSON.");
    }
}

internal class ManifestDependencyArrayConverter : JsonConverter<ManifestDependency[]>
{
    public override ManifestDependency[] Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected start of array.");

        var dependencies = new List<ManifestDependency>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) break;

            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected start of object.");

            string? uniqueID = null;
            string? minimumVersion = null;
            bool? isRequired = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) break;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString()!.ToLowerInvariant();
                    reader.Read();
                    switch (propertyName)
                    {
                        case "uniqueid":
                            uniqueID = reader.GetString();
                            break;
                        case "minimumversion":
                            minimumVersion = reader.GetString();
                            break;
                        case "isrequired":
                            if (reader.TokenType is JsonTokenType.True or JsonTokenType.False)
                            {
                                isRequired = reader.GetBoolean();
                            }
                            else if (reader.TokenType == JsonTokenType.String)
                            {
                                var value = reader.GetString()!;
                                isRequired = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                            }

                            break;
                    }
                }
            }

            dependencies.Add(new ManifestDependency(uniqueID!, minimumVersion, isRequired ?? true));
        }

        return dependencies.ToArray();
    }

    public override void Write(Utf8JsonWriter writer, ManifestDependency[] value, JsonSerializerOptions options)
    {
        throw new InvalidOperationException("This converter does not write JSON.");
    }
}

public class SemVersionConverter : JsonConverter<SemVersion>
{
    public override SemVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected a string value for SemanticVersion, but got {reader.TokenType}.");

        var versionString = reader.GetString();
        if (string.IsNullOrEmpty(versionString)) throw new JsonException("Version string is null or empty.");

        try
        {
            return SemVersion.Parse(versionString, SemVersionStyles.Any);
        }
        catch (Exception ex)
        {
            throw new JsonException($"Failed to parse version string '{versionString}': {ex.Message}", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, SemVersion? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.ToString());
    }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    Converters =
    [
        typeof(ManifestDependencyArrayConverter),
        typeof(SemVersionConverter),
        typeof(UpdateKeysConverter)
    ],
    GenerationMode = JsonSourceGenerationMode.Default
)]
[JsonSerializable(typeof(Manifest))]
[JsonSerializable(typeof(ManifestContentPackFor))]
[JsonSerializable(typeof(ManifestDependency))]
public partial class ManifestContent : JsonSerializerContext;