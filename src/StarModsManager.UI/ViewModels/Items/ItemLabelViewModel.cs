using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using StarModsManager.Api;

namespace StarModsManager.ViewModels.Items;

public partial class ItemLabelViewModel : ViewModelBase
{
    public readonly string SavePath;

    [ObservableProperty]
    private Color _borderColor;

    public ItemLabelViewModel(string? title = default) : this(title ?? "Hidden", Colors.LightBlue, [])
    {
    }

    public ItemLabelViewModel(string title, Color borderColor, ObservableCollection<string> labelItems)
    {
        Title = title;
        SavePath = Path.Combine(Services.ModLabelsPath, $"{title}.json");
        if (!File.Exists(SavePath))
        {
            BorderColor = borderColor;
            Items = labelItems;
        }
        else
        {
            var model = JsonSerializer.Deserialize(File.ReadAllText(SavePath),
                FullItemLabelContext.Default.FullItemLabelViewModel);
            BorderColor = Color.FromUInt32(model!.BorderColor);
            Items = new(model.Items);
        }

        Items.CollectionChanged += (_, _) => Save();
    }

    public string Title { get; set; }

    [JsonIgnore] public ObservableCollection<string>? Items { get; set; }

    partial void OnBorderColorChanged(Color oldValue, Color newValue)
    {
        if (oldValue == newValue) return;
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
        Items = [..viewModel.Items ?? []];
    }

    public uint BorderColor { get; set; }
    public List<string> Items { get; set; }
}