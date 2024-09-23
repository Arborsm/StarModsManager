using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using StarModsManager.Common.Main;
using StarModsManager.ViewModels.Pages;

namespace StarModsManager.Views.Pages;

public partial class ProofreadPageView : UserControl
{
    public ProofreadPageView()
    {
        InitializeComponent();
        ProofreadDataGrid.ItemsSource = Services.GetViewModel<ProofreadPageViewModel>().ModLangsView;
    }
    
    private void ComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ProofreadDataGrid.ItemsSource = Services.GetViewModel<ProofreadPageViewModel>().ModLangsView;
    }

    private void ProofreadDataGrid_OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit) return;
        var dataGrid = (DataGrid) sender!;
        var item = (ModLang) dataGrid.SelectedItem!;
        Services.GetViewModel<ProofreadPageViewModel>().AddEditedLang(item);
    }
}