using System.Collections.ObjectModel;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharpCompress.Archives;
using StarModsManager.Api.SMAPI;

namespace StarModsManager.ViewModels.Customs;

public partial class InstallModViewModel : ViewModelBase
{
    public readonly List<string> Files = [];
    private readonly ObservableCollection<ModFile> _modFiles = [];

    public InstallModViewModel(IEnumerable<IStorageItem> files)
    {
        ModFileSource = new HierarchicalTreeDataGridSource<ModFile>([])
        {
            Columns =
            {
                new CheckBoxColumn<ModFile>("Select", file => file.IsModFolder),
                new HierarchicalExpanderColumn<ModFile>(
                    new TextColumn<ModFile, string>("File Name", file => file.Name)
                    {
                        Options =
                        {
                            MaxWidth = new GridLength(380),
                            TextWrapping = TextWrapping.WrapWithOverflow
                        }
                    },
                    file => file.Children,
                    isExpandedSelector: file => file.IsExpanded),
                new TextColumn<ModFile, string>("Size", file => file.Size)
            }
        };

        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (path == null || !ArchiveFactory.IsArchive(path, out _)) continue;
            Files.Add(path);
            _modFiles.Add(CreateModFileFromZip(file));
        }

        ModFileSource.Items = _modFiles;
    }

    public HierarchicalTreeDataGridSource<ModFile> ModFileSource { get; }

    [RelayCommand]
    private void Install()
    {
        SmapiModInstaller.Install(Files);
    }

    private static ModFile CreateModFileFromZip(IStorageItem file)
    {
        var rootModFile = new ModFile
        {
            Name = Path.GetFileNameWithoutExtension(file.Name),
            Size = FormatFileSize(file.GetBasicPropertiesAsync().GetAwaiter().GetResult().Size)
        };
        
        var path = file.TryGetLocalPath() ?? string.Empty;
        using var archive = ArchiveFactory.Open(path);
        foreach (var entry in archive.Entries) AddEntryToModFile(rootModFile, entry);
        
        return rootModFile;
    }

    private static void AddEntryToModFile(ModFile parent, IArchiveEntry entry)
    {
        if (entry.Key is null) return;
        var pathParts = entry.Key.Split('/');
        var currentParent = parent;
        var processedNodes = new Dictionary<ModFile, ModFile>();

        for (var i = 0; i < pathParts.Length; i++)
        {
            var part = pathParts[i];
            if (string.IsNullOrEmpty(part)) continue;

            var existingChild = currentParent.Children.FirstOrDefault(c => c.Name == part);
            if (existingChild == null)
            {
                var newChild = new ModFile
                {
                    Name = part,
                    Size = i == pathParts.Length - 1 ? FormatFileSize((ulong?)entry.Size) : string.Empty
                };
                currentParent.Children.Add(newChild);
                processedNodes[newChild] = currentParent;
                currentParent = newChild;
            }
            else
            {
                processedNodes.TryAdd(existingChild, currentParent);
                currentParent = existingChild;
            }
        }

        foreach (var node in processedNodes.Keys.Where(node => node.IsValidModFolder()))
        {
            node.IsModFolder = true;
            if (processedNodes.TryGetValue(node, out var parentNode)) parentNode.IsExpanded = true;
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

public partial class ModFile : ObservableObject
{
    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    public bool IsModFolder { get; set; }
    public required string Name { get; init; }
    public required string Size { get; init; }
    public ObservableCollection<ModFile> Children { get; } = [];
}

public static class ModFileExtensions
{
    public static bool IsValidModFolder(this ModFile folder)
    {
        return string.IsNullOrEmpty(folder.Size) && folder.Children
            .Any(child => child.Name.Equals("manifest.json", StringComparison.OrdinalIgnoreCase));
    }
}