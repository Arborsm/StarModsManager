using Avalonia.Controls;

namespace StarModsManager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
    }

    public static MainWindow Instance { get; private set; } = null!;
}