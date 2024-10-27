using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using StarModsManager.Common.Main;
using StarModsManager.ViewModels.Pages;
using StarModsManager.Views.Pages;

namespace StarModsManager.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        NavigationFactory = new NavigationFactory(this);
    }
    
    public NavigationFactory NavigationFactory { get; }
}

public class NavigationFactory(MainWindowViewModel owner) : INavigationPageFactory
{
    public MainWindowViewModel Owner { get; } = owner;

    public Control GetPage(Type srcType)
    {
        return null!;
    }

    public Control GetPageFromObject(object target)
    {
        return (string)target switch
        {
            "Main" => new MainPageView { DataContext = ServiceLocator.Resolve<MainPageViewModel>()},
            "Download" => new DownloadPageView { DataContext = ServiceLocator.Resolve<DownloadPageViewModel>()},
            "Trans" => new TransPageView { DataContext = ServiceLocator.Resolve<TransPageViewModel>()},
            "Check" => new ProofreadPageView { DataContext = ServiceLocator.Resolve<ProofreadPageViewModel>()},
            _ => new UserControl()
        };
    }
}