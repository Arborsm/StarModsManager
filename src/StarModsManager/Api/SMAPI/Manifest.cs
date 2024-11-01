using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using StardewModdingAPI;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Serialization.Models;

namespace StarModsManager.Api.SMAPI;

using System.Collections.Generic;
using System.Text.Json.Serialization;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class Manifest : IManifest
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    [JsonConverter(typeof(SemanticVersionConverter))]
    public ISemanticVersion Version { get; set; } = new SemanticVersion("1.0.0");

    [JsonConverter(typeof(SemanticVersionConverter))]
    public ISemanticVersion? MinimumApiVersion { get; set; }

    [JsonConverter(typeof(SemanticVersionConverter))]
    public ISemanticVersion? MinimumGameVersion { get; set; }

    public string? EntryDll { get; set; }

    [JsonConverter(typeof(ManifestContentPackForConverter))]
    public IManifestContentPackFor? ContentPackFor { get; set; }

    [JsonConverter(typeof(ManifestDependencyArrayConverter))]
    public IManifestDependency[] Dependencies { get; set; } = [];

    [JsonConverter(typeof(UpdateKeysConverter))]
    public string[] UpdateKeys { get; set; } = [];

    public string UniqueID { get; set; } = string.Empty;

    [JsonExtensionData]
    public IDictionary<string, object> ExtraFields { get; set; } = new Dictionary<string, object>();
}

internal class UpdateKeysConverter : JsonConverter<string[]>
{
    public override string[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected start of array for UpdateKeys.");
        }

        var updateKeys = new List<string>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                updateKeys.Add(reader.GetInt32().ToString());
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                updateKeys.Add(reader.GetString() ?? string.Empty);
            }
            else
            {
                throw new JsonException($"Unexpected token type in UpdateKeys: {reader.TokenType}");
            }
        }

        return updateKeys.ToArray();
    }

    public override void Write(Utf8JsonWriter writer, string[] value, JsonSerializerOptions options) => 
        throw new InvalidOperationException("This converter does not write JSON.");
}

internal class ManifestDependencyArrayConverter : JsonConverter<IManifestDependency[]>
{
    public override ManifestDependency[] Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected start of array.");
        }

        var dependencies = new List<ManifestDependency>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected start of object.");
            }

            string? uniqueID = null;
            string? minimumVersion = null;
            bool? isRequired = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

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

    public override void Write(Utf8JsonWriter writer, IManifestDependency[] value, JsonSerializerOptions options) => 
        throw new InvalidOperationException("This converter does not write JSON.");
}

public class ManifestContentPackForConverter : JsonConverter<IManifestContentPackFor>
{
    public override IManifestContentPackFor? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize(ref reader, ManifestContent.Default.ManifestContentPackFor);
    }

    public override void Write(Utf8JsonWriter writer, IManifestContentPackFor value, JsonSerializerOptions options) => 
        throw new InvalidOperationException("This converter does not write JSON.");
}

public class SemanticVersionConverter : JsonConverter<ISemanticVersion>
{
    public override ISemanticVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected a string value for SemanticVersion, but got {reader.TokenType}.");
        }

        var versionString = reader.GetString();
        if (string.IsNullOrEmpty(versionString))
        {
            throw new JsonException("Version string is null or empty.");
        }

        try
        {
            return new SemanticVersion(versionString, allowNonStandard: true);
        }
        catch (Exception ex)
        {
            throw new JsonException($"Failed to parse version string '{versionString}': {ex.Message}", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, ISemanticVersion? value, JsonSerializerOptions options) => 
        throw new InvalidOperationException("This converter does not write JSON.");
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
        typeof(ManifestContentPackForConverter),
        typeof(SemanticVersionConverter)
    ],
    GenerationMode = JsonSourceGenerationMode.Default
)]
[JsonSerializable(typeof(Manifest))]
[JsonSerializable(typeof(ManifestContentPackFor))]
public partial class ManifestContent : JsonSerializerContext;