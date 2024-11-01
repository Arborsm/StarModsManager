using System.Collections.ObjectModel;
using System.IO.Compression;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using StarModsManager.Api;

namespace StarModsManager.ViewModels.Customs;

public partial class InstallModViewModel : ViewModelBase
{
    private readonly List<string> _files = [];
    private readonly ObservableCollection<ModFile> _modFiles = [];

    public InstallModViewModel()
    {
        _modFiles.Add(CreateModFileFromZip(new FileInfo("E:\\Downloads\\SMAPI-develop.zip")));
        ModFileSource.Items = _modFiles;
    }

    public InstallModViewModel(IEnumerable<IStorageItem> files)
    {
        foreach (var file in files)
        {
            if (!Path.GetExtension(file.Name).Equals(".zip", StringComparison.OrdinalIgnoreCase)) continue;
            var path = file.TryGetLocalPath();
            if (path is not null) _files.Add(path);
            _modFiles.Add(CreateModFileFromZip(file));
            ModFileSource.Items = _modFiles;
        }
    }

    public HierarchicalTreeDataGridSource<ModFile> ModFileSource { get; } = new([])
    {
        Columns =
        {
            new CheckBoxColumn<ModFile>("Select", file => !file.IsNotModFolder),
            new HierarchicalExpanderColumn<ModFile>(
                new TextColumn<ModFile, string>("File Name", file => file.Name)
                {
                    Options =
                    {
                        MaxWidth = new GridLength(380),
                        TextWrapping = TextWrapping.WrapWithOverflow
                    }
                }, file => file.Children, isExpandedSelector: file => file.IsNotModFolder),
            new TextColumn<ModFile, string>("Size", file => file.Size)
        }
    };

    [RelayCommand]
    private void Install()
    {
        _files.ForEach(ModsHelper.Install);
    }


    private static ModFile CreateModFileFromZip(IStorageItem zipFile)
    {
        var rootModFile = new ModFile
        {
            Name = Path.GetFileNameWithoutExtension(zipFile.Name),
            Size = FormatFileSize(zipFile.GetBasicPropertiesAsync().GetAwaiter().GetResult().Size)
        };

        using var zipArchive = ZipFile.OpenRead(zipFile.TryGetLocalPath() ?? string.Empty);
        foreach (var entry in zipArchive.Entries) AddEntryToModFile(rootModFile, entry);

        return rootModFile;
    }

    private static ModFile CreateModFileFromZip(FileInfo zipFile)
    {
        var rootModFile = new ModFile
        {
            Name = Path.GetFileNameWithoutExtension(zipFile.Name),
            Size = FormatFileSize((ulong)zipFile.Length)
        };

        using var zipArchive = ZipFile.OpenRead(zipFile.FullName);
        foreach (var entry in zipArchive.Entries) AddEntryToModFile(rootModFile, entry);

        return rootModFile;
    }

    private static void AddEntryToModFile(ModFile parent, ZipArchiveEntry entry)
    {
        var pathParts = entry.FullName.Split('/');
        var currentParent = parent;

        for (var i = 0; i < pathParts.Length; i++)
        {
            var part = pathParts[i];
            if (string.IsNullOrEmpty(part)) continue;
            if (part == "manifest.json") currentParent.IsNotModFolder = false;
            var existingChild = currentParent.Children.FirstOrDefault(c => c.Name == part);
            if (existingChild == null)
            {
                var newChild = new ModFile
                {
                    Name = part,
                    Size = i == pathParts.Length - 1 ? FormatFileSize((ulong?)entry.Length) : ""
                };
                currentParent.Children.Add(newChild);
                currentParent = newChild;
            }
            else
            {
                currentParent = existingChild;
            }
        }
    }

    private static string FormatFileSize(ulong? bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        var counter = 0;
        if (bytes == null) return string.Empty;
        var number = (decimal)bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:n1} {suffixes[counter]}";
    }
}

public class ModFile
{
    public bool IsNotModFolder { get; set; } = true;
    public required string Name { get; init; }
    public required string Size { get; init; }
    public ObservableCollection<ModFile> Children { get; } = [];
}