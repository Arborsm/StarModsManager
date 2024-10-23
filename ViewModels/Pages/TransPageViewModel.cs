using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarModsManager.Api;
using StarModsManager.Common.Main;
using StarModsManager.Common.Mods;
using StarModsManager.Common.Trans;
using StarDebug = StarModsManager.Api.StarDebug;

namespace StarModsManager.ViewModels.Pages;

public partial class TransPageViewModel : ViewModelBase
{
    public TransPageViewModel()
    {
        var mods = ModData.Instance.I18LocalMods
            .Where(it => it.GetUntranslatedMap().Count > 0)
            .Select(it => new ToTansMod(true, it));
        Mods = new ObservableCollection<ToTansMod>(mods);
    }

    [ObservableProperty]
    private int _maxProgress;
    [ObservableProperty]
    private int _progress;
    [ObservableProperty]
    private bool _isIndeterminate;
    [ObservableProperty]
    private bool _isFinished;
    public ObservableCollection<ToTansMod> Mods { get; }
    public ObservableCollection<TansDoneMod> DoneMods { get; } = [];
    public CancellationTokenSource CancellationTokenSource { get; set; } = null!;

    [RelayCommand]
    private async Task ReloadAsync()
    {
        Mods.Clear();
        await Task.Run(() => ModData.Instance.I18LocalMods
            .AsQueryable()
            .Where(it => it.GetUntranslatedMap().Count > 0)
            .Select(it => new ToTansMod(true, it))
            .ForEach(mod => Dispatcher.UIThread.Invoke(() => Mods.Add(mod))));
    }

    public async Task TranslateAsync()
    {
        if (Services.TransConfig.IsBackup)
        {
            CreateBackup();
        }
        try
        {
            ModData.Instance.IsMismatchedTokens = false;
            IsFinished = false;
            var mods = Mods.Where(it => it.IsChecked).Select(it => it.LocalMod).ToArray();
            await Translator.Instance.ProcessDirectoriesAsync(mods, CancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            SMMHelper.Notification("操作已取消");
        }
        catch (Exception? e)
        {
            StarDebug.Error(e);
        }
        finally
        {
            IsIndeterminate = false;
            IsFinished = true; 
            Progress = 0;
        }

        if (ModData.Instance.IsMismatchedTokens)
        {
            SMMHelper.Notification("发现符号匹配错误，建议到校对页面修复");
        }
    }

    public void Clear()
    {
        DoneMods.Clear();
    }
    
    private static void CreateBackup()
    {
        var tempPath = Services.BackupTempDir;
        ModData.Instance.I18LocalMods
            .Where(it => it.GetUntranslatedMap().Count > 0)
            .ForEach(it => BackupMod(it, tempPath));

        var zipFileName = $"backup-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.zip";
        tempPath.CreateZipBackup(zipFileName);
        Directory.Delete(tempPath, true);
    }

    private static void BackupMod(LocalMod mod, string tempPath)
    {
        var i18NFolderPath = Path.Combine(mod.PathS, "i18n");
        var files = Directory.GetFiles(i18NFolderPath, "*", SearchOption.AllDirectories);

        var backupPath = Path.Combine(tempPath, mod.Name.SanitizePath(), "i18n");
        Directory.CreateDirectory(backupPath);

        foreach (var file in files)
        {
            var newPath = file.Replace(i18NFolderPath, backupPath);
            File.Copy(file, newPath, true);
        }
    }
}

public class ToTansMod(bool isChecked, LocalMod localMod)
{
    public LocalMod LocalMod { get; } = localMod;
    public bool IsChecked { get; set; } = isChecked;
    public string Name { get; } = localMod.Name;
    public string Keys { get; } = localMod.GetUntranslatedMap().Count.ToString();
}

public class TansDoneMod(string sourceText, string targetText)
{
    public string SourceText { get; } = sourceText;
    public string TargetText { get; } = targetText;
}