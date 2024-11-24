using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using StarModsManager.Api;

namespace StarModsManager.Config;

public abstract class ConfigBase : ObservableObject
{
    private bool _isSaving;
    protected string? AdditionName = null;
    protected bool IsLoaded;

    protected ConfigBase()
    {
        PropertyChanged += Save;
    }

    protected abstract JsonTypeInfo GetJsonTypeInfo();

    private string GetConfigFilePath()
    {
        var additionName = string.Empty;
        if (this is TransApiConfig) additionName = "_" + AdditionName;
        return Path.Combine(Services.AppSavingPath, GetType().Name + additionName + ".json");
    }

    private void Save(object? sender, PropertyChangedEventArgs e)
    {
        if (_isSaving) return;
        _isSaving = true;

        try
        {
            var json = JsonSerializer.Serialize(this, GetJsonTypeInfo());
            File.WriteAllText(GetConfigFilePath(), json);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save config file: {FilePath}", GetConfigFilePath());
        }
        finally
        {
            _isSaving = false;
        }
    }

    protected static T? Load<T>(JsonTypeInfo jsonTypeInfo, string? additionName = null) where T : ConfigBase
    {
        var filePath = Path.Combine(Services.AppSavingPath,
            typeof(T).Name + (additionName == null ? "" : $"_{additionName}") + ".json");

        if (File.Exists(filePath))
            try
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize(json, jsonTypeInfo) as T;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load config file: {FilePath}", filePath);
            }

        return null;
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(MainConfig))]
[JsonSerializable(typeof(ProofreadConfig))]
[JsonSerializable(typeof(TransApiConfig))]
[JsonSerializable(typeof(TransConfig))]
internal partial class ConfigContent : JsonSerializerContext;