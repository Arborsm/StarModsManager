using System.Windows.Input;

namespace StarModsManager.ViewModels.Customs;

public class CustomNotificationViewModel
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public string? ButtonText { get; init; }
    public ICommand? ButtonCommand { get; init; }
}