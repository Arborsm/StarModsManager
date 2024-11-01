using FluentAvalonia.UI.Controls;
using StarModsManager.Main;
using StarModsManager.ViewModels.Pages;
using StarModsManager.Views.Pages;

namespace StarModsManager.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public NavigationFactory NavigationFactory { get; } = new();
}

public class NavigationFactory : INavigationPageFactory
{

    public Control GetPage(Type srcType)
    {
        return null!;
    }

    public Control GetPageFromObject(object target)
    {
        return (target as string) switch
        {
            "Main" => new MainPageView { DataContext = ServiceLocator.Resolve<MainPageViewModel>() },
            "Download" => new DownloadPageView { DataContext = ServiceLocator.Resolve<DownloadPageViewModel>() },
            "Trans" => new TransPageView { DataContext = ServiceLocator.Resolve<TransPageViewModel>() },
            "Check" => new ProofreadPageView { DataContext = ServiceLocator.Resolve<ProofreadPageViewModel>() },
            "Settings" => new SettingsPageView { DataContext = ServiceLocator.Resolve<SettingsPageViewModel>() },
            _ => new UserControl()
        };
    }
}