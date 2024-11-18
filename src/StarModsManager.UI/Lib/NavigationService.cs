using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using FluentAvalonia.UI.Navigation;

namespace StarModsManager.Lib;

public class NavigationService
{
    public const string Main = "Main";
    public const string Download = "Download";
    public const string Update = "Update";
    public const string Trans = "Trans";
    public const string Check = "Check";
    public const string Settings = "Settings";
    public static readonly NavigationService Instance = new();
    
    private NavigationService()
    {
    }

    private Frame? _frame;

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
}