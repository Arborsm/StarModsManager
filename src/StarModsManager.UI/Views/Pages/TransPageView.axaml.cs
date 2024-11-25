using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using Serilog;
using StarModsManager.Assets;
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
        var vm = ViewModelService.Resolve<TransPageViewModel>();
        vm.CancellationTokenSource = new CancellationTokenSource();
        Content = new TranslatingView
        {
            DataContext = vm
        };
    }

    private async void SettingButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var vm = new TransSettingViewModel();
            var dialog = new ContentDialog
            {
                Title = Lang.TranslatingSettings,
                Content = new TransSettingView
                {
                    DataContext = vm
                },
                PrimaryButtonText = Lang.Save,
                PrimaryButtonCommand = vm.SaveCommand,
                CloseButtonText = Lang.Close
            };

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in ApiSettingButton Click");
        }
    }
}