using StarModsManager.ViewModels;
using StarModsManager.ViewModels.Pages;
using DownloadManagerViewModel = StarModsManager.ViewModels.Customs.DownloadManagerViewModel;

namespace StarModsManager;

public static class ViewModelService
{
    private static readonly Dictionary<Type, Lazy<object>> LazyFactories = new();

    static ViewModelService()
    {
        RegisterAllViewModels();
    }

    private static void RegisterAllViewModels()
    {
        RegisterInternal(typeof(MainPageViewModel), () => new MainPageViewModel());
        RegisterInternal(typeof(DownloadPageViewModel), () => new DownloadPageViewModel());
        RegisterInternal(typeof(TransPageViewModel), () => new TransPageViewModel());
        RegisterInternal(typeof(ProofreadPageViewModel), () => new ProofreadPageViewModel());
        RegisterInternal(typeof(SettingsPageViewModel), () => new SettingsPageViewModel());
        RegisterInternal(typeof(UpdatePageViewModel), () => new UpdatePageViewModel());
        RegisterInternal(typeof(MainViewModel), () => new MainViewModel());
        RegisterInternal(typeof(DownloadManagerViewModel), () => new DownloadManagerViewModel());
    }

    private static void RegisterInternal(Type type, Func<object> factory, bool isSingleton = true)
    {
        if (isSingleton)
            LazyFactories[type] = new Lazy<object>(factory);
        else
            LazyFactories[type] = new Lazy<object>(() => new Func<object>(factory));
    }

    private static object Resolve(Type type)
    {
        if (LazyFactories.TryGetValue(type, out var lazyFactory))
        {
            var service = lazyFactory.Value;
            return service is Func<object> factory ? factory() : service;
        }

        throw new InvalidOperationException($"Service of type {type} is not registered.");
    }

    public static T Resolve<T>() where T : class => (T)Resolve(typeof(T));

    private static bool? _isInDesignMode;
    public static bool IsInDesignMode
    {
        get
        {
            _isInDesignMode ??= Design.IsDesignMode;
            return _isInDesignMode.Value;
        }
    }
}