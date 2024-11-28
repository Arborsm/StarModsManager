using System.Windows.Input;
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

    private class LifeCycle(Action dmAction) : ILifeCycle
    {
        void ILifeCycle.Reset()
        {
            ViewModelService.Reset();
        }

        void ILifeCycle.ShowDownloadManager()
        {
            dmAction();
        }

        void ILifeCycle.AddDownload(string url)
        {
            ViewModelService.Resolve<DownloadManagerViewModel>().AddDownload(url);
        }
    }

    private class PopUp(Control control) : IPopUp
    {
        void IPopUp.ShowFlyout(object flyout)
        {
            (flyout as Flyout)?.ShowAt(control);
        }
    }

    private class Notification(WindowNotificationManager manager) : INotification
    {
        static Notification()
        {
            if (UpdateChecker.ShouldSkipCheck()) return;
            UpdateChecker.CheckUpdate();
        }

        void INotification.Close(object? o)
        {
            if (o != null) manager.Close(o);
        }

        void INotification.CloseAll()
        {
            manager.CloseAll();
        }

        void INotification.Show(object content, Severity severity,
            TimeSpan? expiration,
            Action? onClick,
            Action? onClose)
        {
            var type = GetInfoType(severity);
            InitExpiration(severity, ref expiration);
            Dispatcher.UIThread.Invoke(() => manager.Show(content, type, expiration, onClick, onClose));
        }

        object INotification.Show(string title, string msg, Severity severity,
            TimeSpan? expiration,
            Action? onClick,
            Action? onClose,
            string? buttonText,
            ICommand? buttonCommand)
        {
            var vm = new CustomNotificationViewModel
            {
                Title = title,
                Message = msg.TrimEndNewLine(),
                ButtonText = buttonText,
                ButtonCommand = buttonCommand
            };
            (this as INotification).Show(vm, severity, expiration, onClick, onClose);
            return vm;
        }

        object? INotification.Show(string message)
        {
            return (this as INotification).Show("Info", message.TrimEndNewLine(), Severity.Informational);
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
}