using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using CommunityToolkit.Mvvm.DependencyInjection;
using FluentAvalonia.UI.Controls;
using StarModsManager.ViewModels.Pages;

namespace StarModsManager.Views.Pages;

// ReSharper disable All
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
        var viewModel = Ioc.Default.GetRequiredService<ProofreadPageViewModel>();
        ProofreadDataGrid.ItemsSource = viewModel.ModLangsView;
    }

    private void ProofreadDataGrid_OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit) return;
        var dataGrid = (DataGrid) sender!;
        var item = (ModLang) dataGrid.SelectedItem!;
        Ioc.Default.GetRequiredService<ProofreadPageViewModel>().AddEditedLang(item);
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
        var proofreadDataGrid = (DataGrid) sender!;
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
    
    private static async Task<object> ShowSaveDialog()
    {
        var saveDialog = new TaskDialog
        {
            Title = "提示",
            Header = "当前有未保存的修改，是否继续？",
            Content = "未保存的修改将会丢失",
            Buttons =
            {
                TaskDialogButton.YesButton,
                new TaskDialogButton("Save", "Save"),
                TaskDialogButton.NoButton
            },
            XamlRoot = MainWindow.Instance
        };
    
        return await saveDialog.ShowAsync();
    }

    private static async Task<bool> HandleDialogResult(object result, ProofreadPageViewModel viewModel)
    {
        switch (result)
        {
            case TaskDialogStandardResult.Yes:
                await Task.Run(viewModel.Clear);
                return true;
            case TaskDialogStandardResult.No:
                return false;
            case "Save":
                await viewModel.Save();
                return true;
            default:
                return true;
        }
    }
}