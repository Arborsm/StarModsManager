using Avalonia.Input;
using Avalonia.Interactivity;
using StarModsManager.ViewModels.Items;

namespace StarModsManager.Views.Items;

public partial class ItemLabelView : UserControl
{
    public ItemLabelView()
    {
        InitializeComponent();
        this.DoubleTapped += InputElement_OnDoubleTapped;
        MainTextBox.LostFocus += MainTextBoxOnLostFocus;
    }

    private void MainTextBoxOnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ItemLabelViewModel vm) return;
        vm.IsEditing = false;
    }

    private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not ItemLabelViewModel vm || vm.Title == ItemLabelViewModel.Hidden) return;
        vm.IsEditing = !vm.IsEditing;
        MainTextBox.Focus();
    }
}