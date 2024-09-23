using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using StarModsManager.Common.Main;

namespace StarModsManager.Common.Config;


public abstract class ConfigBase : ObservableObject;

public class ConfigManager<T> : ObservableObject where T : ConfigBase, new()
{
    private readonly T _config = null!;
    private readonly string _additionName;

    public T Config
    {
        get => _config;
        private init => SetProperty(ref _config, value);
    }

    private ConfigManager(string? additionName = null)
    {
        _additionName = additionName == null ? string.Empty : "_" + additionName;
        Config = Load();
        Config.PropertyChanged += (_, _) => Save();
    }

    public static ConfigManager<T> GetInstance(string? additionName = null)
    {
        return new ConfigManager<T>(additionName);
    }

    private T Load()
    {
        var configFilePath = GetConfigFilePath();
        if (!File.Exists(configFilePath))
        {
            Directory.CreateDirectory(Services.AppSavingPath);
            return new T();
        }

        var json = File.ReadAllText(configFilePath);
        return JsonConvert.DeserializeObject<T>(json)!;
    }

    private void Save()
    {
        var configFilePath = GetConfigFilePath();
        var json = JsonConvert.SerializeObject(Config, Formatting.Indented);
        File.WriteAllText(configFilePath, json);
    }

    private string GetConfigFilePath()
    {
        return Path.Combine(Services.AppSavingPath, typeof(T).Name + _additionName + ".json");
    }
}