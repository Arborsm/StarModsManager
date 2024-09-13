using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using StarModsManager.ViewModels;
using StarModsManager.Views.Pages;

namespace StarModsManager.Views;

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
        if (e.SelectedItem is NavigationViewItem)
        {
            var pg = new MainPageView();
            (sender as NavigationView)!.Content = pg;
        }
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}