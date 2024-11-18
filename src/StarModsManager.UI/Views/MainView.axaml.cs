using Avalonia;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using StarModsManager.Api;
using StarModsManager.Lib;
using StarModsManager.ViewModels;
using StarModsManager.ViewModels.Customs;
using StarModsManager.Views.Customs;

namespace StarModsManager.Views;

public partial class MainView : UserControl
{
    private bool _isDesktop;
    private readonly FlyoutBase _flyout;

    public MainView()
    {
        InitializeComponent();

        DownloadItem.Tapped += (_, _) => _flyout?.ShowAt(DownloadItem);
        _flyout = new Flyout
        {
            Placement = PlacementMode.RightEdgeAlignedBottom,
            PlacementConstraintAdjustment = PopupPositionerConstraintAdjustment.All,
            Content = new DownloadManagerView(ViewModelService.Resolve<DownloadManagerViewModel>())
        };
        Services.PopUp = new PopUp(this, () => _flyout.ShowAt(DownloadItem));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isDesktop = TopLevel.GetTopLevel(this) is Window;
        var vm = ViewModelService.Resolve<MainViewModel>();
        DataContext = vm;
        FrameView.NavigationPageFactory = vm.NavigationFactory;
        NavigationService.Instance.SetFrame(FrameView);
        InitializeNavigationPages();
        FrameView.Navigated += OnFrameViewNavigated;
        MainNav.ItemInvoked += OnNavigationViewItemInvoked;
        MainNav.BackRequested += OnNavigationViewBackRequested;
        var manager = new WindowNotificationManager(TopLevel.GetTopLevel(this))
        {
            Margin = new Thickness(0, 25, 5, 0)
        };
        Services.Notification = new Notification(manager);
    }

    private void InitializeNavigationPages()
    {
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var o in MainNav.MenuItems)
            {
                if (o is not NavigationViewItem nvi) continue;
                if (!_isDesktop) nvi.Classes.Add("SampleAppNav");
            }

            MainNav.SettingsItem.Tag = NavigationService.Settings;
            MainNav.SelectedItem = MainNav.MenuItems.ElementAt(0);
            if (OperatingSystem.IsBrowser()) MainNav.Classes.Add("SampleAppNav");

            FrameView.NavigateFromObject((MainNav.SelectedItem as Control)?.Tag);
        });
    }

    private void OnFrameViewNavigated(object sender, NavigationEventArgs e)
    {
        var page = e.Content as Control;
        var dc = page?.DataContext;

        MainPageViewModelBase? thisPage = null;

        if (dc is MainPageViewModelBase pageBase) thisPage = pageBase;

        foreach (NavigationViewItem nvi in MainNav.MenuItems)
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

public class PopUp(Control control, Action downloadManager) : IPopUp
{
    public void ShowDownloadManager() => downloadManager();
    public void AddDownload(string url) => ViewModelService.Resolve<DownloadManagerViewModel>().AddDownload(url);
    public void ShowFlyout(object flyout) => (flyout as Flyout)?.ShowAt(control);
}

public class Notification(WindowNotificationManager manager) : Api.INotification
{
    public void Close(object? o)
    { 
        if (o is not null)
        {
            manager.Close(o);
        }
    }

    public void CloseAll() => manager.CloseAll();

    public void Show(object content, Severity severity)
    {
        var type = GetInfoType(severity);
        Dispatcher.UIThread.Invoke(() => manager.Show(content, type));
    }

    public object Show(string title, string message, Severity severity,
        TimeSpan? expiration = null,
        Action? onClick = null,
        Action? onClose = null)
    {
        var type = GetInfoType(severity);
        if (type == NotificationType.Error && expiration is null) expiration = TimeSpan.Zero;
        var msg = new Avalonia.Controls.Notifications.Notification(title, message, type, expiration, onClick, onClose);
        Dispatcher.UIThread.Invoke(() =>
        {
            manager.Show(msg);
        });
        return msg;
    }

    public object Show(string message) => Show("Info", message, Severity.Informational);

    private static NotificationType GetInfoType(Severity severity) => severity switch
    {
        Severity.Informational => NotificationType.Information,
        Severity.Success => NotificationType.Success,
        Severity.Warning => NotificationType.Warning,
        Severity.Error => NotificationType.Error,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null)
    };
}