using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using StarModsManager.Common.Main;
using StarModsManager.ViewModels.Customs;
using StarModsManager.ViewModels.Pages;
using StarModsManager.Views.Customs;

namespace StarModsManager.Views.Pages;

public partial class TransPageView : UserControl
{
    public TransPageView()
    {
        InitializeComponent();
    }
    
    private void TransButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var vm = ServiceLocator.Resolve<TransPageViewModel>();
        vm.CancellationTokenSource = new CancellationTokenSource();
        this.Content = new TranslatingView
        {
            DataContext = vm
        };
    }

    private async void SettingButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var vm = new TransSettingViewModel();
        var dialog = new ContentDialog
        {
            Title = "Translating Settings",
            Content = new TransSettingView
            {
                DataContext = vm
            },
            PrimaryButtonText = "Save",
            PrimaryButtonCommand = vm.SaveCommand,
            CloseButtonText = "Close"
        };

        await dialog.ShowAsync();
    }
}