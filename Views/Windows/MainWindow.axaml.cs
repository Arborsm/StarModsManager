using System.Threading.Tasks;
using Avalonia.Controls;
using ReactiveUI;
using StarModsManager.ViewModels;

namespace StarModsManager.Views.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // this.WhenActivated(action => action(ViewModel!.ShowDialog.RegisterHandler(DoShowDialogAsync)));
    }

    private async Task DoShowDialogAsync(InteractionContext<ModListViewModel, ModViewModel?> interaction)
    {
        var dialog = new ModListWindow
        {
            DataContext = interaction.Input
        };
        
        var result = await dialog.ShowDialog<ModViewModel?>(this);
        interaction.SetOutput(result);
    }
}