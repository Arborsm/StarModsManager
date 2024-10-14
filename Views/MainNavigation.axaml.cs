using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using FluentAvalonia.UI.Controls;
using StarModsManager.ViewModels.Pages;
using StarModsManager.Views.Pages;

namespace StarModsManager.Views;

public partial class MainNavigation : UserControl
{
    public MainNavigation()
    {
        InitializeComponent();
        var nv = this.FindControl<NavigationView>("MainNav");
        nv!.SelectionChanged += OnMainNavSelectionChanged;
        nv.SelectedItem = nv.MenuItems.ElementAt(0);
    }

    private static void OnMainNavSelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (e.IsSettingsSelected)
        {
            var pg = new SettingsPageView { DataContext = Ioc.Default.GetRequiredService<SettingsPageViewModel>() };
            (sender as NavigationView)!.Content = pg;
        }
        else if (e.SelectedItem is NavigationViewItem navigationViewItem)
        {
            var pg = (navigationViewItem.Tag as string) switch
            {
                "Main" => new MainPageView { DataContext = Ioc.Default.GetRequiredService<MainPageViewModel>() },
                "Download" => new DownloadPageView { DataContext = Ioc.Default.GetRequiredService<DownloadPageViewModel>() },
                "Trans" => new TransPageView { DataContext = Ioc.Default.GetRequiredService<TransPageViewModel>() },
                "Check" => new ProofreadPageView { DataContext = Ioc.Default.GetRequiredService<ProofreadPageViewModel>() },
                _ => new UserControl()
            };

            (sender as NavigationView)!.Content = pg;
        }
    }
}