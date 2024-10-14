using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
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
        var selectedApi = ((TransSettingViewModel)this.DataContext!).SelectedApi;
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
}