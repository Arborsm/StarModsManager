using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using StarModsManager.ViewModels;

namespace StarModsManager.Views;

public partial class ModView : UserControl
{
    public ModView()
    {
        InitializeComponent();
    }

    private void OpenUrl_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ModViewModel modViewModel)
        {
            modViewModel.OpenUrl();
        }
    }

    private async void Refresh_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ModViewModel modViewModel)
        {
            await modViewModel.LoadCover(true);
        }
    }
}