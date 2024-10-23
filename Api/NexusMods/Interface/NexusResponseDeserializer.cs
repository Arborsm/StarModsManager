﻿using System.Net.Http;
using Newtonsoft.Json;
using RestEase;
using StarModsManager.Api.NexusMods.Limit;

namespace StarModsManager.Api.NexusMods.Interface;

public class NexusResponseDeserializer : ResponseDeserializer
{
    private static T DeserializeJson<T>(string content)
    {
        return JsonConvert.DeserializeObject<T>(content) ?? throw new InvalidOperationException("Can't deserialize the response content.");
    }
    
    public override T Deserialize<T>(string? content, HttpResponseMessage response, ResponseDeserializerInfo info)
    {
        if (string.IsNullOrEmpty(content)) throw new InvalidOperationException("The response content is null.");
        
        RateLimits.DailyLimit = GetHeaderValue("x-rl-daily-limit", int.Parse);
        RateLimits.DailyRemaining = GetHeaderValue("x-rl-daily-remaining", int.Parse);
        RateLimits.DailyReset = GetHeaderValue("x-rl-daily-reset", DateTimeOffset.Parse);
        RateLimits.HourlyLimit = GetHeaderValue("x-rl-hourly-limit", int.Parse);
        RateLimits.HourlyRemaining = GetHeaderValue("x-rl-hourly-remaining", int.Parse);
        RateLimits.HourlyReset = GetHeaderValue("x-rl-hourly-reset", DateTimeOffset.Parse);

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
                throw new InvalidOperationException($"The response includes unexpected {name} value: '{value}' can't be converted to {typeof(T).FullName}.", ex);
            }
        }
    }
}