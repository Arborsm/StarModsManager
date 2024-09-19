using System.Reflection;
using StarModsManager.Api;
using StarModsManager.ViewModels;

namespace StarModsManager.Common.Main;

public class Services
{
    private static readonly Services Instance = new();
    private readonly Dictionary<Type, Lazy<IViewModel>> _viewModels = new();

    private Services()
    {
        RegisterViewModels();
    }

    private void RegisterViewModels()
    {
        Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IViewModel).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
            .ForEach(RegisterViewModel);
    }

    private void RegisterViewModel(Type type)
    {
        _viewModels[type] = new Lazy<IViewModel>(() => (IViewModel) Activator.CreateInstance(type)!);
    }

    public static T GetViewModel<T>() where T : class, IViewModel
    {
        if (Instance._viewModels.TryGetValue(typeof(T), out var lazyViewModel))
        {
            return (T)lazyViewModel.Value;
        }
        throw new InvalidOperationException($"ViewModel of type {typeof(T)} is not registered.");
    }
}