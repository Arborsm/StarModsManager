using Avalonia.Controls.Templates;
using StarModsManager.ViewModels;
using StarModsManager.ViewModels.Customs;
using StarModsManager.ViewModels.Items;
using StarModsManager.Views.Customs;
using StarModsManager.Views.Items;

namespace StarModsManager;

public class ViewLocator : IDataTemplate
{
    public Control Build(object? data) => data switch
    {
        ModViewModel => new ModView(),
        ItemLabelViewModel => new ItemLabelView(),
        BitmapViewModel => new BitmapView(),
        ModDetailViewModel => new ModDetailView(),
        DownloadItemViewModel => new DownloadItemView(),
        _ => throw new NotImplementedException()
    };

    public bool Match(object? data) => data is ViewModelBase;
}