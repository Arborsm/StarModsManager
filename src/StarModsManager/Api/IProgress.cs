using StarModsManager.Trans;

namespace StarModsManager.Api;

public interface IProgress
{
    public int Progress { get; set; }
    public int MaxProgress { get; set; }
    public bool IsIndeterminate { get; set; }
    public Progress<int> ProgressBar { get; set; }

    void AddDoneMods(TansDoneMod tansDoneMod);
}