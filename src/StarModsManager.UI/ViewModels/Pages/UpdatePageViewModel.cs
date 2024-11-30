using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Semver;
using StarModsManager.Api;
using StarModsManager.Api.NexusMods;
using StarModsManager.Api.NexusMods.Interface;
using StarModsManager.Api.SMAPI;
using StarModsManager.Api.SMAPI.Model;
using StarModsManager.Assets;
using StarModsManager.Lib;
using StarModsManager.Mods;

namespace StarModsManager.ViewModels.Pages;

public partial class UpdatePageViewModel : MainPageViewModelBase
{
    private static readonly string SaveFile = Path.Combine(Services.AppSavingPath, "CanUpdateMods.json");
    private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(10);

    public UpdatePageViewModel()
    {
        if (!Services.MainConfig.AutoCheckUpdates) Init();
    }

    public static List<ModEntry>? ModEntries { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NotEmpty))]
    [NotifyPropertyChangedFor(nameof(ShowNull))]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    public partial bool IsLoading { get; set; } = true;

    public override string NavHeader => NavigationService.Update;
    public ObservableCollection<ToUpdateMod> Mods { get; set; } = [];
    private bool NotEmpty => !IsLoading && Mods.Count != 0;
    public bool ShowNull => !IsLoading && !NotEmpty;

    public void Init()
    {
        var anyChange = false;
        if (File.Exists(SaveFile))
        {
            var json = File.ReadAllText(SaveFile);
            var cacheData = JsonSerializer.Deserialize<CacheData<ToUpdateModJson>>(json,
                ToUpdateModJsonContent.Default.CacheDataToUpdateModJson);
            if (cacheData != null && DateTime.Now - cacheData.Timestamp < _cacheTimeout)
            {
                foreach (var it in cacheData.Data)
                {
                    if (!ModsHelper.Instance.LocalModsMap.TryGetValue(it.UniqueID, out var mod)) continue;
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        Mods.Add(new ToUpdateMod(mod, true)
                        {
                            LatestVersion = it.LatestVersion
                        });
                    });
                    if (it.LatestVersion.ComparePrecedenceTo(mod.Manifest.Version) < 0)
                    {
                        anyChange = true;
                        break;
                    }
                }

                FinishInit();
            }
        }

        if (Mods.Count == 0 || anyChange) Refresh();
    }

    [RelayCommand(CanExecute = nameof(NotEmpty))]
    private void Refresh()
    {
        IsLoading = true;
        ViewModelService.Resolve<MainViewModel>().ToUpdateModsCount = 0;
        Dispatcher.UIThread.Invoke(Mods.Clear);
        var mods = ModsHelper.Instance.LocalModsMap.Values
            .Where(it => !string.IsNullOrEmpty(it.OnlineMod.ModId))
            .GroupBy(m => m.OnlineMod.ModId)
            .Select(g => (g.Key, g.First()))
            .Select(mod => new ToUpdateMod(mod.Item2))
            .ToList();
        Task.Run(async () =>
        {
            ModEntries = await SMAPI.GetModUpdateData();
            await mods.ForEachAsync(async mod => await mod.UpdateLatestVersionAsync().ContinueWith(task =>
            {
                if (!task.Result.CanUpdate) return;
                Dispatcher.UIThread.Invoke(() => Mods.Add(task.Result));
                ViewModelService.Resolve<MainViewModel>().ToUpdateModsCount++;
            }), 2);
            var cacheData = new CacheData<ToUpdateModJson>(DateTime.Now, Mods
                .Where(it => it.CanUpdate).Select(it => new ToUpdateModJson(it.UniqueID, it.LatestVersion!)));
            var json = JsonSerializer.Serialize(cacheData, ToUpdateModJsonContent.Default.CacheDataToUpdateModJson);
            await File.WriteAllTextAsync(SaveFile, json);
            FinishInit();
        });
    }

    private void FinishInit()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            ViewModelService.Resolve<MainViewModel>().ToUpdateModsCount = Mods.Count(it => it.CanUpdate);
            IsLoading = false;
        });
    }

    [RelayCommand(CanExecute = nameof(NotEmpty))]
    private void Download()
    {
        var mod = Mods
            .Where(m => m.IsChecked)
            .Select(m => (m.LocalMod.OnlineMod.ModId, m.LatestVersion))
            .Select(g => Task.Run(async () => await NexusDownload.DownloadLatestModAsync(g.ModId, g.Item2)))
            .ToList();
        Services.Notification?.Show(Lang.FetchingModLink);
        Task.WhenAll(mod);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true,
    Converters = [typeof(UnixDateTimeConverter)])]
[JsonSerializable(typeof(CacheData<ToUpdateModJson>))]
[JsonSerializable(typeof(List<ToUpdateModJson>))]
[JsonSerializable(typeof(ToUpdateModJson))]
public partial class ToUpdateModJsonContent : JsonSerializerContext;

public class CacheData<T>
{
    public CacheData()
    {
    }

    public CacheData(DateTime time, IEnumerable<T> data)
    {
        Timestamp = time;
        Data = data.ToList();
    }

    public DateTime Timestamp { get; set; }
    public List<T> Data { get; set; } = null!;
}

public class ToUpdateModJson
{
    public ToUpdateModJson()
    {
    }

    public ToUpdateModJson(string id, SemVersion latestVersion)
    {
        UniqueID = id;
        LatestVersion = latestVersion;
    }

    public string UniqueID { get; set; } = null!;

    [JsonConverter(typeof(SemVersionConverter))]
    public SemVersion LatestVersion { get; set; } = null!;
}

public partial class ToUpdateMod(LocalMod localMod, bool isChecked = false) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUpdate))]
    public partial SemVersion? LatestVersion { get; set; }

    public LocalMod LocalMod => localMod;
    public bool CanUpdate => LocalMod.Manifest.Version.ComparePrecedenceTo(LatestVersion) < 0;
    public bool IsChecked { get; set; } = isChecked;
    public string Name { get; } = localMod.Manifest.Name;
    public string UniqueID { get; } = localMod.Manifest.UniqueID;
    public SemVersion CurrentVersion { get; } = localMod.Manifest.Version;

    [RelayCommand]
    private async Task OpenDownloadPageAsync()
    {
        var url = await NexusDownload.Instance.GetModDownloadUrlAsync(LocalMod.OnlineMod.Url);
        if (url is null) return;
        PlatformHelper.OpenFileOrUrl(url);
    }

    public async Task<ToUpdateMod> UpdateLatestVersionAsync()
    {
        var notGetVersionTimes = ModsHelper.GetModNotGetVersionTimes(LocalMod.Manifest.UniqueID);
        if (notGetVersionTimes >= 3) return this;
        if (UpdatePageViewModel.ModEntries != null)
            LatestVersion = UpdatePageViewModel.ModEntries
                .Where(m => m.Id == LocalMod.Manifest.UniqueID && !string.IsNullOrEmpty(m.Metadata.Main.Version))
                .Select(m => SemVersion.Parse(m.Metadata.Main.Version))
                .FirstOrDefault();

        if (LatestVersion != null) return this;
        await Task.Delay(Random.Shared.Next(50, 500));
        LatestVersion = await NexusMod.CreateAsync(LocalMod.OnlineMod.Url, false)
            .ContinueWith(t => t.Result.GetModVersion());
        if (LatestVersion == null) await ModsHelper.AddModNotGetVersionTimes(LocalMod.Manifest.UniqueID);

        return this;
    }

    partial void OnLatestVersionChanged(SemVersion? oldValue, SemVersion? newValue)
    {
        if (newValue is null || newValue == oldValue) return;
        IsChecked = CanUpdate;
    }
}