using Avalonia.Interactivity;
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
        if (ViewModelService.IsInDesignMode) return;
        await ViewModelService.Resolve<TransPageViewModel>().TranslateAsync();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModelService.Resolve<TransPageViewModel>();
        vm.CancellationTokenSource.Cancel();
        vm.Clear();
        Content = new TransPageView
        {
            DataContext = vm
        };
    }
}