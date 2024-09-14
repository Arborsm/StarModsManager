using Avalonia.Controls;

namespace StarModsManager.Views.Windows;

public partial class MainWindow : Window
{
    public static MainWindow Instance = null!;
    
    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
        // this.WhenActivated(action => action(ViewModel!.ShowDialog.RegisterHandler(DoShowDialogAsync)));
    }
}