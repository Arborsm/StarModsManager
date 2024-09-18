using Newtonsoft.Json;
using StarModsManager.Common.Main;

namespace StarModsManager.Common.Config;

public static class ConfigManager<T> where T : new()
{
    public static T Load(string? additionName = null)
    {
        additionName = additionName == null ? string.Empty : "_" + additionName;
        var configFilePath = Path.Combine(Program.AppSavingPath, typeof(T).Name + additionName + ".json");
        if (!File.Exists(configFilePath))
        {
            Directory.CreateDirectory(Program.AppSavingPath);
            return new T();
        }

        var json = File.ReadAllText(configFilePath);
        return JsonConvert.DeserializeObject<T>(json)!;
    }

    public static void Save(T config, string? additionName = null)
    {
        additionName = additionName == null ? string.Empty : "_" + additionName;
        var configFilePath = Path.Combine(Program.AppSavingPath, typeof(T).Name + additionName + ".json");
        var json = JsonConvert.SerializeObject(config, Formatting.Indented);
        File.WriteAllText(configFilePath, json);
    }
}