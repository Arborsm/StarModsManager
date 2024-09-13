using Avalonia.Controls;

namespace StarModsManager.Views.Windows;

public partial class ModListWindow : Window
{
    public ModListWindow()
    {
        InitializeComponent();
        //this.WhenActivated(action => action(ViewModel!.BuyMusicCommand.Subscribe(Close)));
    }
}