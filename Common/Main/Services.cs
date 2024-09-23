using System.Reflection;
using Avalonia.Controls;
using StarModsManager.Api;
using StarModsManager.Common.Config;
using StarModsManager.ViewModels;

namespace StarModsManager.Common.Main;

public class Services
{
    public static readonly string AppSavingPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StarModsManager");
    public static MainConfig MainConfig => ConfigManager<MainConfig>.GetInstance().Config;
    public static TransConfig TransConfig => ConfigManager<TransConfig>.GetInstance().Config;
    public static ProofreadConfig ProofreadConfig => ConfigManager<ProofreadConfig>.GetInstance().Config;
    public static TransApiConfig TransApiConfig => ConfigManager<TransApiConfig>.GetInstance().Config;
    
    private static readonly Services Instance = new();
    private readonly Dictionary<Type, Lazy<IViewModel>> _viewModels = new();
    private static bool? _isInDesignMode;

    public static bool IsInDesignMode
    {
        get
        {
            _isInDesignMode ??= Design.IsDesignMode;
            return _isInDesignMode.Value;
        }
    }

    private Services()
    {
        RegisterViewModels();
    }

    private void RegisterViewModels()
    {
        Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IViewModel).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
            .ForEach(type => _viewModels[type] = new Lazy<IViewModel>(() => (IViewModel)Activator.CreateInstance(type)!));
    }

    public static T GetViewModel<T>() where T : class, IViewModel
    {
        if (Instance._viewModels.TryGetValue(typeof(T), out var lazyViewModel)) return (T)lazyViewModel.Value;
        throw new InvalidOperationException($"ViewModel of type {typeof(T)} is not registered.");
    }
}