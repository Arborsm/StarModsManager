using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.DependencyInjection;
using FluentAvalonia.UI.Controls;
using StarModsManager.Common.Trans;
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
        var vm = Ioc.Default.GetRequiredService<TransPageViewModel>();
        vm.CancellationTokenSource = new CancellationTokenSource();
        this.Content = new TranslatingView
        {
            DataContext = vm
        };
    }

    private async void SettingButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Translating Settings",
            PrimaryButtonText = "Save",
            Content = new TransSettingView
            {
                DataContext = new TransSettingViewModel()
            }
        };

        await dialog.ShowAsync();
    }
}