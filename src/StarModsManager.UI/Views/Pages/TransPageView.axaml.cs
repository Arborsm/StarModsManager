using Avalonia.Interactivity;
using Serilog;
using StarModsManager.Api;
using StarModsManager.Assets;
using StarModsManager.Lib;
using StarModsManager.Trans;
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
        if (!Translator.Instance.IsAvailable)
        {
            Services.Notification?.Show(Lang.Warning, Lang.TranslationNotAvailable, Severity.Warning);
            return;
        }

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
            await NavigationService.ShowTranslationSetting();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in TransPageView");
        }
    }
}