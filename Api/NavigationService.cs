using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using FluentAvalonia.UI.Navigation;

namespace StarModsManager.Api;

public class NavigationService
{
    public static readonly NavigationService Instance = new();
    
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
    
    private Frame? _frame;
}