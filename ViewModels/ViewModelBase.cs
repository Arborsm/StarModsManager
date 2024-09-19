using CommunityToolkit.Mvvm.ComponentModel;
using StarModsManager.Common.Main;

namespace StarModsManager.ViewModels;

public class ViewModelBase : ObservableObject;

/// <summary>
/// 实现此接口的ViewModel可被 Services 类自动注册。
/// </summary>
/// <remarks>
/// 这个接口作为一个标记接口，用于识别应用程序中的 ViewModel。
/// 实现此接口的类将被自动注册到 Services 类中，并可以通过 Services.GetViewModel&lt;T>() 方法检索。
/// </remarks>
/// <seealso cref="Services"/>
public interface IViewModel;