using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.DependencyInjection;
using Newtonsoft.Json;
using StarModsManager.Api;
using StarModsManager.Common.Config;
using StarModsManager.Common.Main;
using StarModsManager.Common.Mods;
using StarModsManager.ViewModels.Pages;

namespace StarModsManager.Common.Trans;

public class Translator
{
    public static Translator Instance { get; } = new();
    public List<ITranslator> Apis { get; } = [new OllamaTrans(), new OpenAITrans()];

    private ITranslator? _translator;
    public ITranslator CurrentTranslator
    {
        get
        {
            if (_translator is null || _translator.Name != Services.TransConfig.ApiSelected)
            {
                _translator = Instance.Apis.First(it => it.Name == Services.TransConfig.ApiSelected);
            }

            return _translator;
        }
    }

    internal async Task ProcessDirectories(IList<LocalMod> toTansMods, CancellationToken token = default)
    {
        var transPageViewModel = Ioc.Default.GetRequiredService<TransPageViewModel>();
        var totalItems = toTansMods.Select(it => it.GetUntranslatedMap().Count).Sum();
        transPageViewModel.MaxProgress = totalItems;
        transPageViewModel.IsIndeterminate = true;

        var progress = new Progress<int>(value => transPageViewModel.Progress = value);

        foreach (var mod in toTansMods)
        {
            if (token.IsCancellationRequested) break;
            await ProcessDirectory(mod.PathS, progress, token);
        }
    }

    private async Task ProcessDirectory(string directoryPath, IProgress<int> progress,
        CancellationToken token)
    {
        var target = Services.TransConfig.Language + ".json";
        var defaultLang = directoryPath.GetDefaultLang();
        var targetLang = directoryPath.GetTargetLang();
        targetLang = targetLang.Sort(defaultLang);
        var path = Path.Combine(directoryPath, "i18n");

        var tran = 
            await ProcessText(defaultLang, targetLang, Services.TransConfig, progress, token);

        if (defaultLang.IsMismatchedTokens(tran)) ModData.Instance.IsMismatchedTokens = true;

        var combined = targetLang
            .Union(tran, new KeyValuePairComparer<string, string>())
            .ToDictionary(k => k.Key, v => v.Value);

        await File.WriteAllTextAsync(Path.Combine(path, target), JsonConvert.SerializeObject(combined), token);
    }

    private async Task<Dictionary<string, string>> ProcessText(
        Dictionary<string, string> map,
        Dictionary<string, string>? mapAllCn,
        TransConfig config,
        IProgress<int> progress,
        CancellationToken token)
    {
        mapAllCn ??= new Dictionary<string, string>();
        var processedMap = new ConcurrentDictionary<string, string>();
        var keys = map.Keys.Except(mapAllCn.Keys).ToList();
        var transPageViewModel = Ioc.Default.GetRequiredService<TransPageViewModel>();
        var processedItems = 0;
        if (Services.TransConfig.IsTurbo)
        {
            var tasks = keys.Select(async key =>
            {
                if (token.IsCancellationRequested) return;
                var result = await TranslateText(map[key], config.PromptText, token);
                if (result == string.Empty) return;
                transPageViewModel.SourceText += map[key] + Environment.NewLine;
                transPageViewModel.TargetText += result + Environment.NewLine;
                Console.WriteLine(result);
                processedMap[key] = result;
                Interlocked.Increment(ref processedItems);
                progress.Report(processedItems);
            });
            await Task.WhenAll(tasks);
        }
        else
        {
            foreach (var key in keys.TakeWhile(_ => !token.IsCancellationRequested))
            {
                if (map[key].Length <= 20) await Task.Delay(config.DelayMs, token);
                var result = await TranslateText(map[key], config.PromptText, token);
                if (result == string.Empty) break;
                transPageViewModel.SourceText += map[key] + Environment.NewLine;
                transPageViewModel.TargetText += result + Environment.NewLine;
                Console.WriteLine(result);
                processedMap[key] = result;
                Interlocked.Increment(ref processedItems);
                progress.Report(processedItems);
            }
        }

        return processedMap.ToDictionary(k => k.Key, v => v.Value);
    }

    internal async Task<string?> TranslateText(string text)
    {
        return ContainsChinese(text) ? text : await TranslateText(text, Services.TransConfig.PromptText);
    }

    private async Task<string> TranslateText(string text, string role, CancellationToken token = default)
    {
        return await CurrentTranslator.StreamCallWithMessage(text, role,
            Services.TransApiConfigs[Services.TransConfig.ApiSelected], token);
    }

    private static bool ContainsChinese(string str)
    {
        return str.Any(ch => ch >= 0x4E00 && ch <= 0x9FFF);
    }
}