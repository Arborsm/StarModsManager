using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using StarModsManager.Api;
using StarModsManager.Mods;
using TransConfig = StarModsManager.Config.TransConfig;

namespace StarModsManager.Trans;

public class Translator
{
    public static Translator Instance { get; } = new();
    public List<ITranslator> Apis { get; } = [new OllamaTrans(), new OpenAITrans()];

    private Translator()
    {
    }

    [field: AllowNull, MaybeNull]
    public ITranslator CurrentTranslator
    {
        get
        {
            if (field is null || field.Name != Services.TransConfig.ApiSelected)
                field = Instance.Apis.First(it => it.Name == Services.TransConfig.ApiSelected);

            return field;
        }
    }

    public async Task ProcessDirectoriesAsync(IList<LocalMod> toTansMods, CancellationToken token = default)
    {
        var totalItems = toTansMods.Select(it => it.GetUntranslatedMap().Count).Sum();
        var iProgress = Services.Progress;
        iProgress.MaxProgress = totalItems;
        iProgress.IsIndeterminate = true;
        iProgress.ProgressBar = new Progress<int>(value => iProgress.Progress = value);

        foreach (var mod in toTansMods)
        {
            if (token.IsCancellationRequested) break;
            await ProcessDirectoryAsync(mod.PathS, iProgress.ProgressBar, token);
        }
    }

    private async Task ProcessDirectoryAsync(string directoryPath, IProgress<int> progress,
        CancellationToken token)
    {
        var target = Services.TransConfig.Language + ".json";
        var defaultLang = directoryPath.GetDefaultLang();
        var targetLang = directoryPath.GetTargetLang();
        var savePath = Path.Combine(Path.Combine(directoryPath, "i18n"), target);

        var tran =
            await ProcessTextAsync(defaultLang, targetLang, Services.TransConfig, progress, token);

        if (defaultLang.IsMismatchedTokens(tran) ?? true) ModsHelper.Instance.IsMismatchedTokens = true;

        var combined = targetLang
            .Union(tran, new KeyValuePairComparer<string, string>())
            .ToDictionary(k => k.Key, v => v.Value);

        combined.Sort(defaultLang);

        var content =
            JsonSerializer.Serialize(combined, TranslationContext.Default.DictionaryStringString);
        await File.WriteAllTextAsync(savePath, content, token);
    }

    private async Task<Dictionary<string, string>> ProcessTextAsync(Dictionary<string, string> map,
        Dictionary<string, string>? mapAllCn,
        TransConfig config,
        IProgress<int> progress,
        CancellationToken token)
    {
        mapAllCn ??= new Dictionary<string, string>();
        var processedMap = new ConcurrentDictionary<string, string>();
        var keys = map.Keys.Except(mapAllCn.Keys).ToList();
        var processedItems = 0;
        if (Services.TransConfig.IsTurbo)
        {
            var tasks = keys.Select(async key =>
            {
                if (token.IsCancellationRequested) return;
                var result = await TranslateTextAsync(map[key], config.PromptText, token);
                if (result == string.Empty) return;
                Services.Progress.AddDoneMods(new TansDoneMod(map[key], result));
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
                var result = await TranslateTextAsync(map[key], config.PromptText, token);
                if (result == string.Empty) break;
                Services.Progress.AddDoneMods(new TansDoneMod(map[key], result));
                Console.WriteLine(result);
                processedMap[key] = result;
                Interlocked.Increment(ref processedItems);
                progress.Report(processedItems);
            }
        }

        return processedMap.ToDictionary(k => k.Key, v => v.Value);
    }

    public async Task<string?> TranslateTextAsync(string text) => 
        ContainsChinese(text) ? text : await TranslateTextAsync(text, Services.TransConfig.PromptText);

    private async Task<string> TranslateTextAsync(string text, string role, CancellationToken token = default) =>
        await CurrentTranslator.StreamCallWithMessageAsync(text, role, Services.TransApiConfigs[Services.TransConfig.ApiSelected], token);

    private static bool ContainsChinese(string str)
    {
        return str.Any(ch => ch >= 0x4E00 && ch <= 0x9FFF);
    }
}

public class TansDoneMod(string sourceText, string targetText)
{
    public string SourceText { get; } = sourceText;
    public string TargetText { get; } = targetText;
}