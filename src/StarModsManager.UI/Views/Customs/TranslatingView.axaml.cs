using Avalonia.Interactivity;
using StarModsManager.Main;
using StarModsManager.ViewModels.Pages;
using StarModsManager.Views.Pages;

namespace StarModsManager.Views.Customs;

public partial class TranslatingView : UserControl
{
    public TranslatingView()
    {
        InitializeComponent();
        Initialized += OnInitialized;
    }

    private static async void OnInitialized(object? sender, EventArgs e)
    {
        if (ServiceLocator.IsInDesignMode) return;
        await ServiceLocator.Resolve<TransPageViewModel>().TranslateAsync();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var vm = ServiceLocator.Resolve<TransPageViewModel>();
        vm.CancellationTokenSource.Cancel();
        vm.Clear();
        Content = new TransPageView
        {
            DataContext = vm
        };
    }
}