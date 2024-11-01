using Avalonia.Controls.Templates;
using StarModsManager.ViewModels;
using StarModsManager.ViewModels.Customs;
using StarModsManager.ViewModels.Items;
using StarModsManager.Views.Customs;
using StarModsManager.Views.Items;

namespace StarModsManager.Main;

public class ViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        return data switch
        {
            ModViewModel => new ModView(),
            ItemLabelViewModel => new ItemLabelView(),
            BitmapViewModel => new BitmapView(),
            ModDetailViewModel => new ModDetailView(),
            _ => throw new NotImplementedException()
        };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}