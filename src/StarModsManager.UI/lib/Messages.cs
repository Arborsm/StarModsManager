using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;

namespace StarModsManager.lib;

public class PickupFilesDialogMessage
{
    public required string Title { get; init; }
    public bool AllowMultiple { get; init; } = false;
    public IReadOnlyList<FilePickerFileType>? FileTypeFilter { get; init; } = [];
    public TaskCompletionSource<IReadOnlyList<IStorageFile>> CompletionSource { get; } = new();
}

public class NotificationMessage
{
    public required string Title { get; init; }
    public required string Message { get; init; }

    public InfoBarSeverity Severity { get; init; } = InfoBarSeverity.Informational;
    public bool IsClosable => true;
    public bool IsIconVisible => true;
}