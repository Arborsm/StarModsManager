using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using FluentAvalonia.UI.Navigation;
using StarModsManager.Assets;
using StarModsManager.ViewModels.Customs;
using StarModsManager.Views.Customs;

namespace StarModsManager.Lib;

public class NavigationService
{
    public const string Main = "Main";
    public const string Download = "Download";
    public const string Update = "Update";
    public const string Trans = "Trans";
    public const string Check = "Check";
    public const string Settings = "Settings";
    public const string ModTools = "ModTools";
    public static readonly NavigationService Instance = new();

    private Frame? _frame;

    private NavigationService()
    {
    }

    public Control? PreviousPage { get; set; }

    public void SetFrame(Frame f)
    {
        _frame = f;
    }

    public void Navigate(Type t)
    {
        _frame?.Navigate(t);
    }

    public void NavigateFromContext(object? dataContext, NavigationTransitionInfo? transitionInfo = null)
    {
        _frame?.NavigateFromObject(dataContext,
            new FrameNavigationOptions
            {
                IsNavigationStackEnabled = true,
                TransitionInfoOverride = transitionInfo ?? new SuppressNavigationTransitionInfo()
            });
    }

    public static async Task ShowTranslationSetting()
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
}