using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using StarModsManager.lib;
using StarModsManager.ViewModels;

namespace StarModsManager.Views;

public partial class MainView : UserControl
{
    private bool _isDesktop;

    public MainView()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.Register<NotificationMessage>(this, ShowNotification);
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

    private void ShowNotification(object recipient, NotificationMessage message)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (message.Severity == InfoBarSeverity.Informational)
                MainInfoBar.Background =
                    Application.Current!.RequestedThemeVariant == ThemeVariant.Light
                        ? Brushes.Azure
                        : Brushes.MediumSlateBlue;
            MainInfoBar.IsClosable = message.IsClosable;
            MainInfoBar.Title = message.Title;
            MainInfoBar.Message = message.Message;
            MainInfoBar.Severity = message.Severity;
            MainInfoBar.IsIconVisible = message.IsIconVisible;
            MainInfoBar.IsOpen = true;
        });
    }

    private void InitializeNavigationPages()
    {
        var menuItems = new List<NavigationViewItemBase>(4);
        var footerItems = new List<NavigationViewItemBase>(1);

        Dispatcher.UIThread.Post(() =>
        {
            List<Page> list =
            [
                new("主页", new SymbolIconSource { Symbol = Symbol.Home }, NavigationService.Main),
                new("下载", new SymbolIconSource { Symbol = Symbol.Download }, NavigationService.Download),
                new("更新", new SymbolIconSource { Symbol = Symbol.Sync }, NavigationService.Update),
                new("翻译", new SymbolIconSource { Symbol = Symbol.Character }, NavigationService.Trans),
                new("校对", new SymbolIconSource { Symbol = Symbol.Edit }, NavigationService.Check)
            ];
            foreach (var nvi in list.Select(pg => new NavigationViewItem
                     {
                         Content = pg.Content,
                         Tag = pg.Tag,
                         IconSource = pg.IconSource
                     }))
            {
                if (!_isDesktop) nvi.Classes.Add("SampleAppNav");
                menuItems.Add(nvi);
            }

            MainNav.MenuItemsSource = menuItems;
            MainNav.FooterMenuItemsSource = footerItems;
            MainNav.SettingsItem.Tag = NavigationService.Settings;
            MainNav.SelectedItem = MainNav.MenuItemsSource.ElementAt(0);

            if (OperatingSystem.IsBrowser())
                MainNav.Classes.Add("SampleAppNav");
            else if (!_isDesktop) MainNav.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;

            FrameView.NavigateFromObject((MainNav.SelectedItem as Control)?.Tag);
        });
    }

    private void OnFrameViewNavigated(object sender, NavigationEventArgs e)
    {
        var page = e.Content as Control;
        var dc = page?.DataContext;

        MainPageViewModelBase? thisPage = null;

        if (dc is MainPageViewModelBase pageBase) thisPage = pageBase;

        foreach (NavigationViewItem nvi in MainNav.MenuItemsSource)
            if (nvi.Tag as string == thisPage?.NavHeader)
                MainNav.SelectedItem = nvi;
        if (FrameView.BackStackDepth > 0 && !MainNav.IsBackButtonVisible)
            AnimateContentForBackButton(true);
        else if (FrameView.BackStackDepth == 0 && MainNav.IsBackButtonVisible) AnimateContentForBackButton(false);
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
}

public class Page(string content, IconSource iconSource, string tag)
{
    public readonly string Content = content;
    public readonly IconSource IconSource = iconSource;
    public readonly string Tag = tag;
}