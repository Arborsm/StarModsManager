using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using StarModsManager.Lib;
using StarModsManager.ViewModels.Pages;
using StarModsManager.Views.Pages;

namespace StarModsManager.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _toUpdateModsCount;

    [ObservableProperty]
    private int _missingDependencyModsCount;

    [ObservableProperty]
    private int _downloadItemsCount;

    public MainViewModel()
    {
        ViewModelService.Resolve<SettingsPageViewModel>().Init();
    }

    public NavigationFactory NavigationFactory { get; } = new();
}

public class NavigationFactory : INavigationPageFactory
{
    public Control GetPage(Type srcType) => null!;

    public Control GetPageFromObject(object target) => (target as string) switch
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
        _ => throw new NotImplementedException($"Cannot get Page from source: {target}")
    };
}