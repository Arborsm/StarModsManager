using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using Serilog;
using StarModsManager.ViewModels.Customs;

namespace StarModsManager.Views.Customs;

public partial class TransSettingView : UserControl
{
    public TransSettingView()
    {
        InitializeComponent();
    }

    private async void ApiSettingButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var selectedApi = ((TransSettingViewModel)DataContext!).SelectedApi;
            var vm = new ApiSettingViewModel(selectedApi);
            var dialog = new ContentDialog
            {
                Title = "Api Settings",
                Content = new ApiSettingView
                {
                    DataContext = vm
                },
                PrimaryButtonText = "Save",
                PrimaryButtonCommand = vm.SaveCommand,
                CloseButtonText = "Close"
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in ApiSettingButton Click");
        }
    }
}