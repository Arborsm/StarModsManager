using Avalonia.Controls;
using Avalonia.Interactivity;
using StarModsManager.Common.Main;
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
        if (Services.IsInDesignMode) return;
        await ServiceLocator.Resolve<TransPageViewModel>().TranslateAsync();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var vm = ServiceLocator.Resolve<TransPageViewModel>();
        vm.CancellationTokenSource.Cancel();
        vm.Clear();
        this.Content = new TransPageView
        {
            DataContext = vm
        };
    }
}