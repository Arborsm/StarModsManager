using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using StarModsManager.Lib;
using StarModsManager.ViewModels.Pages;
using StarModsManager.Views.Pages;

namespace StarModsManager.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        ViewModelService.Resolve<SettingsPageViewModel>().Init();
    }

    [ObservableProperty]
    public partial int DownloadItemsCount { get; set; }

    [ObservableProperty]
    public partial int MissingDependencyModsCount { get; set; }

    [ObservableProperty]
    public partial int ToUpdateModsCount { get; set; }

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
            NavigationService.Main => new MainPageView
            {
                DataContext = ViewModelService.Resolve<MainPageViewModel>()
            },
            NavigationService.Download => new DownloadPageView
            {
                DataContext = ViewModelService.Resolve<DownloadPageViewModel>()
            },
            NavigationService.Update => new UpdatePageView
            {
                DataContext = ViewModelService.Resolve<UpdatePageViewModel>()
            },
            NavigationService.Trans => new TransPageView
            {
                DataContext = ViewModelService.Resolve<TransPageViewModel>()
            },
            NavigationService.Check => new ProofreadPageView
            {
                DataContext = ViewModelService.Resolve<ProofreadPageViewModel>()
            },
            NavigationService.Settings => new SettingsPageView
            {
                DataContext = ViewModelService.Resolve<SettingsPageViewModel>()
            },
            NavigationService.ModTools => new ModToolsPageView
            {
                DataContext = ViewModelService.Resolve<ModToolsPageViewModel>()
            },
            _ => throw new NotImplementedException($"Cannot get Page from source: {target}")
        };
    }
}