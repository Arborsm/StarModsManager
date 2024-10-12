using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.DependencyInjection;
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
        await Ioc.Default.GetRequiredService<TransPageViewModel>().Translate();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        var vm = Ioc.Default.GetRequiredService<TransPageViewModel>();
        vm.CancellationTokenSource.Cancel();
        this.Content = new TransPageView
        {
            DataContext = vm
        };
    }
}