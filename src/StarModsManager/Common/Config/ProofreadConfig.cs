using System.Text.Json.Serialization.Metadata;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StarModsManager.Common.Config;

public partial class ProofreadConfig : ConfigBase
{
    [ObservableProperty]
    private bool _canSort;

    [ObservableProperty]
    private bool _enableHeaderResizing;

    [ObservableProperty]
    private bool _hasBorder;

    [ObservableProperty]
    private bool _isFilter;

    [ObservableProperty]
    private bool _isVisibleHeader = true;
    
    protected override JsonTypeInfo GetJsonTypeInfo()
    {
        return ConfigContent.Default.ProofreadConfigContent;
    }

    protected override IConfigContent GetContent()
    {
        return new ProofreadConfigContent
        {
            CanSort = this.CanSort,
            EnableHeaderResizing = this.EnableHeaderResizing,
            HasBorder = this.HasBorder,
            IsFilter = this.IsFilter,
            IsVisibleHeader = this.IsVisibleHeader
        };
    }

    protected override void LoadFromJson(object loadedConfig)
    {
        if (loadedConfig is not ProofreadConfigContent config) return;
        this.CanSort = config.CanSort;
        this.EnableHeaderResizing = config.EnableHeaderResizing;
        this.HasBorder = config.HasBorder;
        this.IsFilter = config.IsFilter;
        this.IsVisibleHeader = config.IsVisibleHeader;
    }
}

public class ProofreadConfigContent : IConfigContent
{
    public bool CanSort { get; set; }
    public bool EnableHeaderResizing { get; set; }
    public bool HasBorder { get; set; }
    public bool IsFilter { get; set; }
    public bool IsVisibleHeader { get; set; }
}