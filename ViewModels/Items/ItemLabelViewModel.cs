using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Avalonia.Media;

namespace StarModsManager.ViewModels.Items;

public class ItemLabelViewModel(string title, Color borderColor) : ViewModelBase
{
    public Color BorderColor { get; set; } = borderColor;
    public string Title { get; set; } = title;
    [JsonIgnore]
    public ObservableCollection<string> Items { get; set; } = [];
    [JsonIgnore]
    public IBrush BorderBrush => new SolidColorBrush(BorderColor);
    public ItemLabelViewModel() : this("Hidden", Colors.LightBlue) { }
}