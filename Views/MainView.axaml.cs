using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using StarModsManager.Api;
using StarModsManager.ViewModels;

namespace StarModsManager.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isDesktop = TopLevel.GetTopLevel(this) is Window;
        var vm = new MainWindowViewModel();
        DataContext = vm;
        FrameView.NavigationPageFactory = vm.NavigationFactory;
        NavigationService.Instance.SetFrame(FrameView);

        InitializeNavigationPages();

        FrameView.Navigated += OnFrameViewNavigated;
        MainNav.ItemInvoked += OnNavigationViewItemInvoked;
        MainNav.BackRequested += OnNavigationViewBackRequested;
    }

    private void InitializeNavigationPages()
    {
        var menuItems = new List<NavigationViewItemBase>(4);
        var footerItems = new List<NavigationViewItemBase>(1);

        Dispatcher.UIThread.Post(() =>
        {
            List<Page> list =
            [
                new("主页", "HomeIcon", "Main"),
                new("下载", "DownloadIcon", "Download"),
                new("更新", "SyncIcon", "Update"),
                new("翻译", "CharacterIcon", "Trans"),
                new("校对", "ProofreadIcon", "Check")
            ];
            foreach (var nvi in list.Select(pg => new NavigationViewItem
                     {
                         Content = pg.Content,
                         Tag = pg.Tag,
                         IconSource = (IconSource?)this.FindResource(pg.IconSource)
                     }))
            {
                if (!_isDesktop) nvi.Classes.Add("SampleAppNav");
                menuItems.Add(nvi);
            }

            MainNav.MenuItemsSource = menuItems;
            MainNav.FooterMenuItemsSource = footerItems;

            if (OperatingSystem.IsBrowser())
            {
                MainNav.Classes.Add("SampleAppNav");
            }
            else if (!_isDesktop)
            {
                MainNav.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
            }

            FrameView.NavigateFromObject((MainNav.MenuItemsSource.ElementAt(0) as Control)?.Tag);
        });
    }

    private void OnFrameViewNavigated(object sender, NavigationEventArgs e)
    {
        if (FrameView.BackStackDepth > 0 && !MainNav.IsBackButtonVisible)
        {
            AnimateContentForBackButton(true);
        }
        else if (FrameView.BackStackDepth == 0 && MainNav.IsBackButtonVisible)
        {
            AnimateContentForBackButton(false);
        }
    }

    

    private void OnNavigationViewBackRequested(object? sender, NavigationViewBackRequestedEventArgs e)
    {
        FrameView.GoBack();
    }

    private static void OnNavigationViewItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer is not NavigationViewItem nvi) return;
        NavigationService.Instance.NavigateFromContext(nvi.Tag, e.RecommendedNavigationTransitionInfo);
    }

    private void AnimateContentForBackButton(bool show)
    {
        MainNav.IsBackButtonVisible = show;
    }

    private bool _isDesktop;
}

public class Page(string content, string iconSource, string tag)
{
    public readonly string Content = content;
    public readonly string IconSource = iconSource;
    public readonly string Tag = tag;
}