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
using INotification = StarModsManager.Api.INotification;

namespace StarModsManager.Views;

public partial class MainView : UserControl
{
    private readonly FlyoutBase _flyout;
    private bool _isDesktop;

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
        Services.PopUp = new PopUp(this);
        Services.LifeCycle = new LifeCycle(() => _flyout.ShowAt(DownloadItem));
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

    public class LifeCycle(Action dmAction) : ILifeCycle
    {
        public void Reset()
        {
            ViewModelService.Reset();
        }

        public void ShowDownloadManager()
        {
            dmAction();
        }

        public void AddDownload(string url)
        {
            ViewModelService.Resolve<DownloadManagerViewModel>().AddDownload(url);
        }
    }
}

public class PopUp(Control control) : IPopUp
{
    public void ShowFlyout(object flyout)
    {
        (flyout as Flyout)?.ShowAt(control);
    }
}

public class Notification(WindowNotificationManager manager) : INotification
{
    public void Close(object? o)
    {
        if (o != null) manager.Close(o);
    }

    public void CloseAll()
    {
        manager.CloseAll();
    }

    public void Show(object content, Severity severity,
        TimeSpan? expiration = null,
        Action? onClick = null,
        Action? onClose = null)
    {
        var type = GetInfoType(severity);
        InitExpiration(severity, ref expiration);
        Dispatcher.UIThread.Invoke(() => manager.Show(content, type, expiration, onClick, onClose));
    }

    public object Show(string title, string msg, Severity severity,
        TimeSpan? expiration = null,
        Action? onClick = null,
        Action? onClose = null)
    {
        var vm = new CustomNotificationViewModel
        {
            Title = title,
            Message = msg
        };
        Show(vm, severity, expiration, onClick, onClose);
        return vm;
    }

    public object Show(string message)
    {
        return Show("Info", message, Severity.Informational);
    }

    private static void InitExpiration(Severity type, ref TimeSpan? expiration)
    {
        expiration ??= type switch
        {
            Severity.Error => TimeSpan.Zero,
            Severity.Warning => TimeSpan.FromSeconds(10),
            _ => TimeSpan.FromSeconds(8)
        };
    }

    private static NotificationType GetInfoType(Severity severity)
    {
        return severity switch
        {
            Severity.Informational => NotificationType.Information,
            Severity.Success => NotificationType.Success,
            Severity.Warning => NotificationType.Warning,
            Severity.Error => NotificationType.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null)
        };
    }
}