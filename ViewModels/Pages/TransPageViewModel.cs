using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarModsManager.Api;
using StarModsManager.Common.Main;
using StarModsManager.Common.Mods;
using StarModsManager.Common.Trans;

namespace StarModsManager.ViewModels.Pages;

public partial class TransPageViewModel : ViewModelBase
{
    public TransPageViewModel()
    {
        Mods = new ObservableCollection<ToTansMod>(
            ModData.Instance.I18LocalMods
                .Where(it => it.GetUntranslatedMap().Count > 0)
                .Select(it => new ToTansMod(true, it)));
    }

    [ObservableProperty]
    private int _maxProgress;
    [ObservableProperty]
    private int _progress;
    [ObservableProperty]
    private bool _isIndeterminate;
    [ObservableProperty]
    private string _sourceText = string.Empty;
    [ObservableProperty]
    private string _targetText = string.Empty;
    [ObservableProperty]
    private bool _isFinished;

    public ObservableCollection<ToTansMod> Mods { get; }
    public CancellationTokenSource CancellationTokenSource { get; set; } = null!;

    [RelayCommand]
    private async Task Reload()
    {
        Mods.Clear();
        await Task.Run(() =>ModData.Instance.I18LocalMods
            .AsQueryable()
            .Where(it => it.GetUntranslatedMap().Count > 0)
            .Select(it => new ToTansMod(true, it))
            .ForEach(mod => Dispatcher.UIThread.Invoke(() => Mods.Add(mod))));
    }

    public async Task Translate()
    {
        if (Services.TransConfig.IsBackup)
        {
            CreateBackup();
        }
        try
        {
            IsFinished = false;
            await Translator.Instance.ProcessDirectories(
                Mods.Where(it => it.IsChecked).Select(it => it.LocalMod).ToArray(), CancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            SMMTools.Notification("操作已取消");
        }
        catch (Exception e)
        {
            StarDebug.Error(e);
        }
        finally
        {
            IsIndeterminate = false;
            IsFinished = true; SourceText = string.Empty;
            TargetText = string.Empty;
            Progress = 0;
        }
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