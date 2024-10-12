using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.DependencyInjection;
using Newtonsoft.Json;
using StarModsManager.Api;
using StarModsManager.Common.Main;
using StarModsManager.Common.Mods;
using StarModsManager.ViewModels.Pages;

namespace StarModsManager.Common.Trans;

public class Translator
{
    public static Translator Instance { get; } = new();
    public readonly ITranslator CurrentTranslator;

    public Translator()
    {
        CurrentTranslator = UpdateConfig();
    }

    public List<ITranslator> Apis { get; } = [new OllamaTrans(), new OpenAITrans()];

    public ITranslator UpdateConfig()
    {
        return Apis.First(it => it.Name == Services.TransConfig.ApiSelected);
    }

    internal async Task<string?> TranslateText(string text)
    {
        return ContainsChinese(text) ? text : await TranslateText(text, Services.TransConfig.PromptText);
    }

    internal async Task<ProcessResult> ProcessDirectories(LocalMod[] toTansMods,
        CancellationToken token = default)
    {
        var transPageViewModel = Ioc.Default.GetRequiredService<TransPageViewModel>();
        var directories = toTansMods.Select(it => it.PathS).ToArray();
        transPageViewModel.MaxProgress = toTansMods.Length;
        transPageViewModel.Progress = 0;
        var tasks = directories.Select(async directory =>
        {
            await ProcessDirectory(directory, token);
            transPageViewModel.Progress++;
        });

        await Task.WhenAll(tasks);
        return ProcessResult.Success;
    }

    private async Task ProcessDirectory(string directoryPath, CancellationToken token)
    {
        var target = Services.TransConfig.Language + ".json";
        var defaultLang = directoryPath.GetDefaultLang();
        var targetLang = directoryPath.GetTargetLang();
        targetLang = targetLang.Sort(defaultLang);
        var path = Path.Combine(directoryPath, "i18n");

        var tran = await ProcessText(defaultLang, targetLang, Services.TransConfig.PromptText, token);

        if (defaultLang.IsMismatchedTokens(tran)) ModData.Instance.IsMismatchedTokens = true;

        var combined = targetLang
            .Union(tran, new KeyValuePairComparer<string, string>())
            .ToDictionary(k => k.Key, v => v.Value);

        await File.WriteAllTextAsync(Path.Combine(path, target), JsonConvert.SerializeObject(combined), token);
    }
    
    private async Task<Dictionary<string, string>> ProcessText(Dictionary<string, string> map,
        Dictionary<string, string>? mapAllCn,
        string role, CancellationToken token)
    {
        mapAllCn ??= new Dictionary<string, string>();
        var processedMap = new ConcurrentDictionary<string, string>();
        var keys = map.Keys.Except(mapAllCn.Keys).ToList();
        var tasks = keys.Select(async key =>
        {
            if (map[key].Length <= 20) await Task.Delay(500, token);
            var result = await TranslateText(map[key], role, token);

            if (result != string.Empty)
            {
                Console.WriteLine(result);
                processedMap[key] = result;
            }
        });

        await Task.WhenAll(tasks);
        return processedMap.ToDictionary(k => k.Key, v => v.Value);
    }
    
    private async Task<string> TranslateText(string text, string role, CancellationToken token = default)
    {
        return await CurrentTranslator.StreamCallWithMessage(text, role, Services.TransApiConfigs[Services.TransConfig.ApiSelected], token);
    }

    private static bool ContainsChinese(string str)
    {
        return str.Any(ch => ch >= 0x4E00 && ch <= 0x9FFF);
    }
}