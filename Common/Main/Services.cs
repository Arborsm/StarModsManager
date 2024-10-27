using System.Reflection;
using Avalonia.Controls;
using StarModsManager.Common.Config;
using StarModsManager.Common.Trans;
using StarModsManager.ViewModels;

namespace StarModsManager.Common.Main;

public static class Services
{
    public static readonly string AppVersion =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";

    public static readonly string AppSavingPath =
        CombineNCheckDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StarModsManager");

    public static readonly string ModCategoriesPath = CombineNCheckDirectory(AppSavingPath, "ModCategories");
    public static readonly string TempDir = CombineNCheckDirectory(Path.GetTempPath(), "StarModsManager");
    public static readonly string BackupTempDir = CombineNCheckDirectory(TempDir, "Backup");
    public static readonly string OnlineModsDir = CombineNCheckDirectory(TempDir, "OnlineMods");
    public static readonly string LogDir = CombineNCheckDirectory(AppSavingPath, "Log");
    public static MainConfig MainConfig => ConfigManager<MainConfig>.GetInstance().Config;
    public static TransConfig TransConfig => ConfigManager<TransConfig>.GetInstance().Config;
    public static ProofreadConfig ProofreadConfig => ConfigManager<ProofreadConfig>.GetInstance().Config;

    public static Dictionary<string, TransApiConfig> TransApiConfigs { get; } =
        Translator.Instance.Apis.ToDictionary(t => t.Name, t => ConfigManager<TransApiConfig>.GetInstance(t.Name).Config);

    private static bool? _isInDesignMode;

    public static bool IsInDesignMode
    {
        get
        {
            _isInDesignMode ??= Design.IsDesignMode;
            return _isInDesignMode.Value;
        }
    }

    private static string CombineNCheckDirectory(params string[] directories)
    {
        var path = Path.Combine(directories);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        return path;
    }
}

public static class ServiceLocator
{
    private static readonly Dictionary<Type, Lazy<object>> LazyFactories = new();

    public static void Register<TInterface, TImplementation>(bool isSingleton = true)
        where TInterface : class
        where TImplementation : class, TInterface, new()
    {
        RegisterInternal<TInterface>(() => new TImplementation(), isSingleton);
    }

    public static void Register<T>(bool isSingleton = true) where T : class, new()
    {
        RegisterInternal(() => new T(), isSingleton);
    }

    public static void Register<TInterface>(Func<TInterface> factory, bool isSingleton = true)
        where TInterface : class
    {
        RegisterInternal(factory, isSingleton);
    }

    private static void RegisterInternal<T>(Func<T> factory, bool isSingleton) where T : class
    {
        if (isSingleton)
        {
            LazyFactories[typeof(T)] = new Lazy<object>(() => factory());
        }
        else
        {
            LazyFactories[typeof(T)] = new Lazy<object>(() => new Func<object>(() => factory()));
        }
    }

    public static void RegisterAllViewModels(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();

        var viewModelTypes =
            assembly.GetTypes().Where(t =>
                t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(MainPageViewModelBase)));

        foreach (var type in viewModelTypes)
        {
            RegisterInternal(type, () => Activator.CreateInstance(type)!, true);
        }
    }

    private static void RegisterInternal(Type type, Func<object> factory, bool isSingleton)
    {
        if (isSingleton)
        {
            LazyFactories[type] = new Lazy<object>(factory);
        }
        else
        {
            LazyFactories[type] = new Lazy<object>(() => new Func<object>(factory));
        }
    }

    public static object Resolve(Type type)
    {
        if (LazyFactories.TryGetValue(type, out var lazyFactory))
        {
            var service = lazyFactory.Value;
            return service is Func<object> factory ? factory() : service;
        }

        throw new InvalidOperationException($"Service of type {type} is not registered.");
    }

    public static T Resolve<T>() where T : class
    {
        return (T)Resolve(typeof(T));
    }

    public static void Clear()
    {
        LazyFactories.Clear();
    }
}