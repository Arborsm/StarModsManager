using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using CommunityToolkit.Mvvm.ComponentModel;
using StarModsManager.Api;

namespace StarModsManager.Common.Config;

public abstract class ConfigBase : ObservableObject
{
    private readonly string? _additionName;
    private bool _isSaving;

    protected ConfigBase(string? additionName = null)
    {
        _additionName = additionName;
        Load();
        PropertyChanged += (_, _) => Save();
    }

    protected abstract JsonTypeInfo GetJsonTypeInfo();

    private void Load()
    {
        var configFilePath = GetConfigFilePath();
        if (!File.Exists(configFilePath)) return;
        var json = File.ReadAllText(configFilePath);
        var loadedConfig = JsonSerializer.Deserialize(json, GetJsonTypeInfo());
        if (loadedConfig != null) LoadFromJson(loadedConfig);
    }
    
    protected abstract IConfigContent GetContent();
    
    protected abstract void LoadFromJson(object loadedConfig);

    private void Save()
    {
        if (_isSaving) return;
        _isSaving = true;
        try
        {
            var configFilePath = GetConfigFilePath();
            var json = JsonSerializer.Serialize(GetContent(), GetJsonTypeInfo());
            File.WriteAllText(configFilePath, json);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private string GetConfigFilePath() => Path.Combine(Services.AppSavingPath, GetType().Name + (_additionName == null ? "" : $"_{_additionName}") + ".json");
}

public interface IConfigContent;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(MainConfigContent))]
[JsonSerializable(typeof(ProofreadConfigContent))]
[JsonSerializable(typeof(TransApiConfigContent))]
[JsonSerializable(typeof(TransConfigContent))]
internal partial class ConfigContent : JsonSerializerContext;