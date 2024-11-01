using Avalonia;
using StarModsManager.ViewModels.Pages;

namespace StarModsManager.Views.Pages;

public partial class MainPageView : UserControl
{
    public MainPageView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        (DataContext as MainPageViewModel)?.ReFreshLabels();
    }
}