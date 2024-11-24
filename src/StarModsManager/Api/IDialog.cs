using Avalonia.Platform.Storage;

namespace StarModsManager.Api;

public interface IDialog
{
    Task<IReadOnlyList<IStorageFile>> ShowPickupFilesDialogAsync(string title, bool allowMultiple,
        IReadOnlyList<FilePickerFileType>? fileTypeFilter = default);

    Task<string?> ShowDownloadDialogAsync();
}