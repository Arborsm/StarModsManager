using System.Text.Json.Serialization.Metadata;

namespace StarModsManager.Config;

public class ProofreadConfig : ConfigBase
{
    public bool CanSort
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool EnableHeaderResizing
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool HasBorder
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsFilter
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsVisibleHeader
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    protected override JsonTypeInfo GetJsonTypeInfo()
    {
        return ConfigContent.Default.ProofreadConfig;
    }
    
    public static ProofreadConfig LoadOrCreate()
    {
        var config = Load<ProofreadConfig>(ConfigContent.Default.ProofreadConfig);
        return config ?? new ProofreadConfig();
    }
}
