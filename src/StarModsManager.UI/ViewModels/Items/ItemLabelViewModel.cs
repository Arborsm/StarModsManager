using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using StarModsManager.Api;

namespace StarModsManager.ViewModels.Items;

public partial class ItemLabelViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isEditing;
    
    private readonly bool _isInit;
    private string SavePath => Path.Combine(Services.ModLabelsPath, $"{Title}.json");

    [ObservableProperty]
    private Color _borderColor;

    public const string Hidden = "Hidden";
    public ItemLabelViewModel() : this("Hidden", [], Colors.LightBlue)
    {
    }

    public ItemLabelViewModel(string title, IEnumerable<ModViewModel> allMods, Color? borderColor = default)
    {
        _isInit = true;
        Title = title;
        if (!File.Exists(SavePath))
        {
            BorderColor = borderColor ?? Colors.LightBlue;
            Items = new ObservableCollection<ModViewModel>([]);
        }
        else
        {
            var model = JsonSerializer.Deserialize(File.ReadAllText(SavePath),
                FullItemLabelContext.Default.FullItemLabelViewModel);
            BorderColor = Color.FromUInt32(model!.BorderColor);
            Items = new ObservableCollection<ModViewModel>(allMods
                .Where(it => model.Items.Any(modId => modId == it.LocalMod!.Manifest.UniqueID)));
        }

        Items.CollectionChanged += (_, _) => Save();
        _isInit = false;
    }

    [ObservableProperty]
    private string _title;

    [JsonIgnore] 
    public ObservableCollection<ModViewModel> Items { get; set; }

    partial void OnTitleChanged(string? oldValue, string newValue)
    {
        if (oldValue == newValue || _isInit) return; 
        Save();
        var filePath = Path.Combine(Services.ModLabelsPath, $"{oldValue}.json");
        if (File.Exists(filePath)) File.Delete(filePath);
        OnPropertyChanged();
    }

    partial void OnBorderColorChanged(Color oldValue, Color newValue)
    {
        if (oldValue == newValue || _isInit) return;
        Save();
    }

    private void Save()
    {
        var fullItem = new FullItemLabelViewModel(this);
        var json = JsonSerializer.Serialize(fullItem, FullItemLabelContext.Default.FullItemLabelViewModel);
        File.WriteAllText(SavePath, json);
    }
}

[JsonSerializable(typeof(List<TitleOnlyViewModel>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class TitleOnlyContext : JsonSerializerContext;

public class TitleOnlyViewModel
{
    public TitleOnlyViewModel()
    {
        Title = string.Empty;
    }

    public TitleOnlyViewModel(ItemLabelViewModel viewModel)
    {
        Title = viewModel.Title;
    }

    public string Title { get; set; }
}

[JsonSerializable(typeof(FullItemLabelViewModel))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class FullItemLabelContext : JsonSerializerContext;

public class FullItemLabelViewModel
{
    public FullItemLabelViewModel()
    {
        Items = [];
    }

    public FullItemLabelViewModel(ItemLabelViewModel viewModel)
    {
        BorderColor = viewModel.BorderColor.ToUInt32();
        Items = [..viewModel.Items.Select(it => it.LocalMod!.Manifest.UniqueID)];
    }

    public uint BorderColor { get; set; }
    public List<string> Items { get; set; }
}