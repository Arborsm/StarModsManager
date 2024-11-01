using CommunityToolkit.Mvvm.ComponentModel;

namespace StarModsManager.ViewModels;

public class ViewModelBase : ObservableObject;

public class MainPageViewModelBase : ViewModelBase
{
    public virtual string NavHeader => null!;
}