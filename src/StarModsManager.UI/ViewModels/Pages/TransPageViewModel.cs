using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarModsManager.Api;
using StarModsManager.Assets;
using StarModsManager.Lib;
using StarModsManager.Mods;
using StarModsManager.Trans;

namespace StarModsManager.ViewModels.Pages;

public partial class TransPageViewModel : MainPageViewModelBase, IProgress
{
    public TransPageViewModel()
    {
        Services.Progress = this;
        var mods = ModsHelper.Instance.I18LocalMods
            .Where(it => it.GetUntranslatedMap().Count > 0)
            .Select(it => new ToTansMod(true, it));
        Mods = new ObservableCollection<ToTansMod>(mods);
    }

    [ObservableProperty]
    public partial bool IsFinished { get; set; }

    public override string NavHeader => NavigationService.Trans;
    public ObservableCollection<ToTansMod> Mods { get; }
    public ObservableCollection<TansDoneMod> DoneMods { get; } = [];
    public CancellationTokenSource CancellationTokenSource { get; set; } = null!;

    [ObservableProperty]
    public partial bool IsIndeterminate { get; set; }

    [ObservableProperty]
    public partial int MaxProgress { get; set; }

    [ObservableProperty]
    public partial int Progress { get; set; }

    public Progress<int> ProgressBar { get; set; } = new();

    public void AddDoneMods(TansDoneMod tansDoneMod)
    {
        Dispatcher.UIThread.Invoke(() => DoneMods.Add(tansDoneMod));
    }

    [RelayCommand]
    private async Task ReloadAsync()
    {
        Mods.Clear();
        await Task.Run(() => ModsHelper.Instance.I18LocalMods
            .Where(it => it.GetUntranslatedMap().Count > 0)
            .Select(it => new ToTansMod(true, it))
            .ForEach(mod => Dispatcher.UIThread.Invoke(() => Mods.Add(mod))));
    }

    public async Task TranslateAsync()
    {
        if (Services.TransConfig.IsBackup) CreateBackup();
        try
        {
            ModsHelper.Instance.IsMismatchedTokens = false;
            IsFinished = false;
            var mods = Mods.Where(it => it.IsChecked).Select(it => it.LocalMod).ToArray();
            await Translator.Instance.ProcessDirectoriesAsync(mods, CancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            Services.Notification?.Show(Lang.OperationCanceled);
        }
        catch (Exception? e)
        {
            SMMDebug.Error(e);
        }
        finally
        {
            IsIndeterminate = false;
            IsFinished = true;
            Progress = 0;
        }

        if (ModsHelper.Instance.IsMismatchedTokens) Services.Notification?.Show(Lang.SymbolError);
    }

    public void Clear()
    {
        DoneMods.Clear();
    }

    private static void CreateBackup()
    {
        var tempPath = Services.BackupTempDir;
        ModsHelper.Instance.I18LocalMods
            .Where(it => it.GetUntranslatedMap().Count > 0)
            .ForEach(it => BackupMod(it, tempPath));
        const string zipFileName = "Localization";
        tempPath.CreateZipBackup(zipFileName);
        Directory.Delete(tempPath, true);
    }

    private static void BackupMod(LocalMod mod, string tempPath)
    {
        var i18NFolderPath = Path.Combine(mod.PathS, "i18n");
        var files = Directory.GetFiles(i18NFolderPath, "*", SearchOption.AllDirectories);

        var tempI18NPath = Path.Combine(tempPath, mod.Manifest.Name.SanitizePath(), "i18n");
        Directory.CreateDirectory(tempI18NPath);

        foreach (var file in files)
        {
            var newPath = file.Replace(i18NFolderPath, tempI18NPath);
            File.Copy(file, newPath, true);
        }
    }
}

public class ToTansMod(bool isChecked, LocalMod localMod)
{
    public LocalMod LocalMod { get; } = localMod;
    public bool IsChecked { get; set; } = isChecked;
    public string Name { get; } = localMod.Manifest.Name;
    public string Keys { get; } = localMod.GetUntranslatedMap().Count.ToString();
}