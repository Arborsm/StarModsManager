using System.Text.Json;
using System.Text.Json.Serialization;

namespace StarModsManager.Api.NexusMods.Interface;

public class UnixDateTimeConverter : JsonConverter<DateTimeOffset?>
{
    private static readonly DateTimeOffset UnixEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        long unixTime;
        if (reader.TokenType == JsonTokenType.Number)
        {
            unixTime = reader.GetInt64();
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            if (!long.TryParse(reader.GetString(), out unixTime))
            {
                throw new JsonException("Cannot convert invalid value to Unix timestamp.");
            }
        }
        else
        {
            throw new JsonException($"Unexpected token parsing date. Expected Number or String, got {reader.TokenType}.");
        }

        if (unixTime < 0)
        {
            throw new JsonException("Cannot convert value that is before Unix epoch of 00:00:00 UTC on 1 January 1970.");
        }

        return UnixEpoch.AddSeconds(unixTime);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            long unixTime = (long)(value.Value.ToUniversalTime() - UnixEpoch).TotalSeconds;
            if (unixTime < 0)
            {
                throw new JsonException("Cannot convert date value that is before Unix epoch of 00:00:00 UTC on 1 January 1970.");
            }
            writer.WriteNumberValue(unixTime);
        }
    }
}