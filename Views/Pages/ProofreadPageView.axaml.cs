using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using StarModsManager.Common.Main;
using StarModsManager.ViewModels.Pages;

namespace StarModsManager.Views.Pages;

public partial class ProofreadPageView : UserControl
{
    public ProofreadPageView()
    {
        InitializeComponent();
        ProofreadDataGrid.AddHandler(PointerWheelChangedEvent, ProofreadDataGrid_OnPointerWheelChanged, RoutingStrategies.Tunnel);
        DisableDefaultScrolling(ProofreadDataGrid);
    }
    
    private void ComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var viewModel = ServiceLocator.Resolve<ProofreadPageViewModel>();
        ProofreadDataGrid.ItemsSource = viewModel.ModLangsView;
    }

    private void ProofreadDataGrid_OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit) return;
        var dataGrid = (DataGrid) sender!;
        var item = (ModLang) dataGrid.SelectedItem!;
        ServiceLocator.Resolve<ProofreadPageViewModel>().AddEditedLang(item);
    }
    
    private static void DisableDefaultScrolling(DataGrid dataGrid)
    {
        var type = typeof(DataGrid);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var eventField = type.GetField("PointerWheelChanged", flags);
        if (eventField != null)
        {
            eventField.SetValue(dataGrid, null);
        }
    }

    private static void ProofreadDataGrid_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        e.Handled = true;
        var proofreadDataGrid = (DataGrid)sender!;
        if (!proofreadDataGrid.GetLogicalChildren().OfType<DataGridCellsPresenter>().Any(p => p.IsFocused))
        {
            proofreadDataGrid.Focus();
        }
        
        var key = e.Delta.Y > 0 ? Key.Up : Key.Down;
        var keyEventArgs = new KeyEventArgs
        {
            RoutedEvent = KeyDownEvent,
            Key = key
        };

        proofreadDataGrid.RaiseEvent(keyEventArgs);
    }

    private void ProofreadDataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        e.Handled = true;
        ((DataGrid)sender!).InvalidateVisual();
    }
}