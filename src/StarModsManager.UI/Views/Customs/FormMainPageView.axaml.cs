namespace StarModsManager.Views.Customs;

public partial class FormMainPageView : UserControl
{
    public FormMainPageView()
    {
        InitializeComponent();
    }

    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        e.Handled = true;
        var dataGrid = (DataGrid)sender!;

        dataGrid.ScrollIntoView(dataGrid.SelectedItem, dataGrid.Columns[0]);
        dataGrid.InvalidateVisual();
    }
}