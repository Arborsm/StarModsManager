namespace StarModsManager.Views.Customs;

public partial class DownloadManagerView : UserControl
{
    public DownloadManagerView()
    {
        InitializeComponent();
    }

    public DownloadManagerView(object? dataContext) : this()
    {
        DataContext = dataContext;
    }
}