using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using StarModsManager.Common.Main;
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
            var pg = new SettingsPageView { DataContext = Services.GetViewModel<SettingsPageViewModel>() };
            (sender as NavigationView)!.Content = pg;
        }
        else if (e.SelectedItem is NavigationViewItem navigationViewItem)
        {
            var pg = (navigationViewItem.Tag as string) switch
            {
                "Main" => new MainPageView { DataContext = Services.GetViewModel<MainPageViewModel>() },
                "Download" => new DownloadPageView { DataContext = Services.GetViewModel<DownloadPageViewModel>() },
                _ => new UserControl()
            };

            (sender as NavigationView)!.Content = pg;
        }
    }
}