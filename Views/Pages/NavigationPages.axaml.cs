using Avalonia.Controls;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using StarModsManager.ViewModels.Pages;
using DownloadPageViewModel = StarModsManager.ViewModels.Pages.DownloadPageViewModel;
using SettingsPageViewModel = StarModsManager.ViewModels.Pages.SettingsPageViewModel;

namespace StarModsManager.Views.Pages;

public partial class NavigationPages : UserControl
{
    public NavigationPages()
    {
        InitializeComponent();
        // Default NavView
        var nv = this.FindControl<NavigationView>("MainNav");
        nv!.SelectionChanged += OnMainNavSelectionChanged;
        nv.SelectedItem = nv.MenuItems.ElementAt(0);
    }

    private void OnMainNavSelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (e.IsSettingsSelected)
        {
            var pg = new SettingsPageView
            {
                DataContext = new SettingsPageViewModel()
            };
            (sender as NavigationView)!.Content = pg;
        }
        else if (e.SelectedItem is NavigationViewItem navigationViewItem)
        {
            UserControl pg = (navigationViewItem.Tag as string) switch
            {
                "Main" => new MainPageView { DataContext = new MainPageViewModel() },
                "Download" => new DownloadPageView { DataContext = new DownloadPageViewModel() },
                _ => new MainPageView { DataContext = new MainPageViewModel() }
            };

            (sender as NavigationView)!.Content = pg;
        }
    }
}