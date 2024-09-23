using System.Collections.Concurrent;
using Newtonsoft.Json;
using StarModsManager.Api;
using StarModsManager.Common.Config;
using StarModsManager.Common.Main;
using StarModsManager.Common.Mods;

namespace StarModsManager.Common.Trans;

public class Translator
{
    public ITranslator CurrentTranslator;

    public Translator()
    {
        FindAllApis();
        CurrentTranslator = UpdateConfig();
    }

    public List<ITranslator> Apis { get; } = new();

    public ITranslator UpdateConfig()
    {
        return Apis.First(it => it.Name == Services.TransConfig.ApiSelected);
    }

    private void FindAllApis()
    {
        var types = GetType().Assembly.GetTypes();
        foreach (var type in types)
        {
            if (!type.GetInterfaces().Contains(typeof(ITranslator))) continue;
            var instance = (ITranslator)Activator.CreateInstance(type)!;
            Apis.Add(instance);
        }
    }

    private static bool ContainsChinese(string str)
    {
        return str.Any(ch => ch >= 0x4E00 && ch <= 0x9FFF);
    }

    internal async Task<string?> TranslateText(string text)
    {
        return ContainsChinese(text) ? text : await TranslateText(text, Services.TransConfig.EnToCn);
    }

    internal async Task<string> TranslateText(string text, string role)
    {
        var token = new CancellationToken(); // Form is not null ? Form.Tsl.Token : 
        return await CurrentTranslator.StreamCallWithMessage(text, role, Services.TransApiConfig, token);
    }

    internal async Task<Dictionary<string, string>> ProcessText(
        Dictionary<string, string> map,
        Dictionary<string, string>? mapAllCn,
        string role)
    {
        mapAllCn ??= new Dictionary<string, string>();
        var processedMap = new ConcurrentDictionary<string, string>();
        var keys = map.Keys.Except(mapAllCn.Keys).ToList();
        //_main.sonProgressPar.Maximum = keys.Count;
        var tasks = keys.Select(async key =>
        {
            if (map[key].Length <= 20) await Task.Delay(500);
            var result = await TranslateText(map[key], role);

            if (result != string.Empty)
            {
                Console.WriteLine(result);
                // Form!.Invoke(Form.SonProgressUpdate(key, processedMap.Keys.ToList(), keys));
                processedMap[key] = result;
            }
        });

        await Task.WhenAll(tasks);
        return processedMap.ToDictionary(k => k.Key, v => v.Value);
    }

    internal async Task<ProcessResult> ProcessDirectories()
    {
        var directories = ModData.Instance.I18LocalMods.Select(it => it.PathS).ToArray();
        var maximum = (float)directories.Length;

        var tasks = directories.Select(async directory =>
        {
            // Debug.Assert(Form is not null, nameof(Form) + " is not null");
            // Form.Invoke(Form.MainProgressUpdate(0, $@"0/{maximum}"));
            await ProcessDirectory(directory);
            // Form.Invoke(Form.MainProgressUpdate(1.0f / maximum, $@"{++i}/{maximum}"));
        });

        await Task.WhenAll(tasks);
        return ProcessResult.Success;
    }

    private async Task ProcessDirectory(string directoryPath)
    {
        var target = Services.TransConfig.Language + ".json";
        var defaultLang = directoryPath.GetDefaultLang();
        var targetLang = directoryPath.GetTargetLang();
        targetLang = targetLang.Sort(defaultLang);
        var path = Path.Combine(directoryPath, "i18n");

        var tran = await ProcessText(defaultLang, targetLang, Services.TransConfig.EnToCn);

        if (defaultLang.IsMismatchedTokens(tran)) ModData.Instance.IsMismatchedTokens = true;

        var combined = targetLang
            .Union(tran, new KeyValuePairComparer<string, string>())
            .ToDictionary(k => k.Key, v => v.Value);

        await File.WriteAllTextAsync(Path.Combine(path, target), JsonConvert.SerializeObject(combined));
    }
}