using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SharpCompress.Archives;
using StarModsManager.Assets;
using StarModsManager.Trans;

namespace StarModsManager.Api.SMAPI;

public class SmapiModInstaller(string fileDir)
{
    private readonly string _targetDirectory = Services.MainConfig.DirectoryPath;
    private readonly string _tempDir = Services.TempDir;
    private ModInstallContext _context = new();

    public static void Install(IEnumerable<string> localPaths)
    {
        var paths = localPaths.ToDictionary(it => it, it => ArchiveFactory.Open(it));
        var msg = paths
            .Select(it => string.Format(Lang.ToInstall, Path.GetFileNameWithoutExtension(it.Key)))
            .Aggregate(string.Empty, (current, text) =>
            {
                var lineBreak = string.IsNullOrEmpty(current) ? current : Environment.NewLine;
                return current + lineBreak + text;
            });
        Services.Notification.Show(Lang.Installing, msg, Severity.Informational);

        var notSupportMsg = new StringBuilder();
        var successMsg = new StringBuilder();
        Task.Run(async () =>
        {
            foreach (var pair in paths)
            {
                var context = await StartInstall(pair, successMsg);
                if (context is { IsNotSupportPack: true }) notSupportMsg.AppendLine(context.ModFolderName);
            }
        }).ContinueWith(_ =>
        {
            if (successMsg.Length != 0)
                Services.Notification.Show(Lang.ModInstallSuccess,
                    successMsg.ToString(), Severity.Success, TimeSpan.FromSeconds(15));

            if (notSupportMsg.Length != 0)
                Services.Notification.Show(Lang.MultiModUpdateNotSupport,
                    notSupportMsg.ToString(), Severity.Warning, TimeSpan.FromMinutes(1));

            Services.LifeCycle.Reset();
        });
    }

    private static async Task<ModInstallContext?> StartInstall(KeyValuePair<string, IArchive> pair,
        StringBuilder successMsg)
    {
        var type = GetArchiveType(pair.Value.Entries);
        switch (type)
        {
            case ArchiveType.Mod:
                var installer = new SmapiModInstaller(pair.Key);
                var context = installer.Install();
                if (!context.IsNotSupportPack)
                    successMsg.AppendLine(string.Format(Lang.InstalledModWithVersion,
                        installer._context.ModFolderName, installer._context.Manifest.Version));

                return context;
            case ArchiveType.Translation:
                var fileName = new FileInfo(pair.Key).Name;
                var tansInstaller = new TranslationPackInstaller(fileName);
                var result = await tansInstaller.ProcessI18NInstallation(pair.Value);
                if (result is null) successMsg.AppendLine(string.Format(Lang.I18NInstallSuccess, fileName));

                return null;
            case ArchiveType.Unknown:
            default:
                throw new ArgumentOutOfRangeException($"{type} is not supported");
        }
    }

    private static ArchiveType GetArchiveType(IEnumerable<IArchiveEntry> entries)
    {
        var validEntries = entries.Where(entry => entry is { IsDirectory: false, Key: not null }).ToArray();

        if (validEntries.Any(entry => entry.Key!.EndsWith("manifest.json", StringComparison.OrdinalIgnoreCase)))
        {
            return ArchiveType.Mod;
        }
        else if (validEntries.Any(entry => entry.Key!.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
        {
            return ArchiveType.Translation;
        }

        return ArchiveType.Unknown;
    }

    private ModInstallContext Install()
    {
        try
        {
            using var archive = ArchiveFactory.Open(fileDir);
            _context = ValidateAndCreateContext(archive);
            if (_context.IsNotSupportPack) return _context;
            CreateTempDirectories();
            ProcessModInstallation(archive);
        }
        catch (InvalidDataException e)
        {
            SMMDebug.Error(e, "Error: Invalid zip file");
        }
        catch (Exception e)
        {
            SMMDebug.Error(e, $"Error occurred during mod installation: {e.Message}");
        }
        finally
        {
            CleanupTempDirectories();
        }

        return _context;
    }

    private ModInstallContext ValidateAndCreateContext(IArchive archive)
    {
        var defaultResult = new ModInstallContext(Path.GetFileName(fileDir));

        var manifestEntries = archive.Entries
            .Where(e => e.Key != null && e.Key.EndsWith("manifest.json", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (manifestEntries.Count == 0)
        {
            Services.Notification.Show(Lang.NotModArchive, Lang.ManifestFileNotFound, Severity.Warning);
            return defaultResult;
        }

        var manifestEntry = manifestEntries.First();
        var manifest = JsonSerializer.Deserialize(manifestEntry.OpenEntryStream(), ManifestContent.Default.Manifest)!;
        var modFolderName = Path.GetDirectoryName(manifestEntry.Key) ?? Path.GetFileNameWithoutExtension(fileDir);
        if (modFolderName.Split('\\').Length > 1) modFolderName = modFolderName.Split('\\').Last();

        var existingMod = ModsHelper.Instance.LocalModsMap.Values
            .FirstOrDefault(mod => mod.Manifest.UniqueID == manifest.UniqueID);

        if (manifestEntries.Count > 1 && existingMod != null) return defaultResult;

        var tempDir = Path.Combine(_tempDir, "ToInstallMod");
        var tempModDir = Path.Combine(_tempDir, modFolderName);
        var destDir = ModsHelper.Instance.LocalModsMap.Values
            .FirstOrDefault(mod => mod.Manifest.UniqueID == manifest.UniqueID)?.PathS;
        var modDir = Path.Combine(_targetDirectory, modFolderName);

        return new ModInstallContext
        {
            IsNotSupportPack = false,
            Manifest = manifest,
            ModFolderName = modFolderName,
            TempDir = tempDir,
            TempModDir = tempModDir,
            DestDir = destDir,
            ModDir = modDir,
            DestDirParent = FindCommonPath(destDir, Services.MainConfig.DirectoryPath)
        };
    }

    private static string? FindCommonPath(string? subPath, string basePath)
    {
        if (string.IsNullOrEmpty(subPath)) return null;
        subPath = subPath.Replace('/', '\\').TrimEnd('\\');
        basePath = basePath.Replace('/', '\\').TrimEnd('\\');

        if (subPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            var subParts = subPath.Split('\\');
            var baseParts = basePath.Split('\\');

            return baseParts.Length < subParts.Length
                ? string.Join("\\", subParts.Take(baseParts.Length + 1))
                : basePath;
        }

        var pathParts = subPath.Split('\\');
        for (var i = 0; i < pathParts.Length - 1; i++)
        {
            var currentPath = string.Join("\\", pathParts.Take(i + 1));
            var nextPath = string.Join("\\", pathParts.Take(i + 2));

            if (currentPath.Equals(basePath, StringComparison.OrdinalIgnoreCase)) return nextPath;
        }

        return string.Empty;
    }

    private void CreateTempDirectories()
    {
        Directory.CreateDirectory(_context.TempDir);
        Directory.CreateDirectory(_context.TempModDir);
    }

    private void ProcessModInstallation(IArchive archive)
    {
        archive.ExtractToDirectory(_context.TempDir);

        if (_context.DestDirParent != null) HandleExistingMod(_context.DestDirParent);

        var dir = Directory.GetParent(
            Directory.EnumerateFileSystemEntries(_context.TempDir, "*", SearchOption.AllDirectories)
                .First(it => it.Contains(_context.ModFolderName) && it.Contains("manifest.json")))!.FullName;
        MoveDirectory(dir, _context.ModDir);

        if (Directory.EnumerateFiles(_context.TempModDir).Any()) RestoreModFiles();

        Log.Information("Mod installed successfully: {Dir}", _context.ModFolderName);
    }

    private static void MoveDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Move(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            MoveDirectory(dir, destSubDir);
        }
    }

    private void HandleExistingMod(string dir)
    {
        Log.Information("Mod already exists: {Dir}", dir);
        if (_context.DestDirParent != null) dir.CreateZipBackup(_context.DestDirParent);

        BackupConfigFile();
        BackupI18NFolder();

        try
        {
            Directory.Delete(dir, true);
        }
        catch (Exception)
        {
            Services.Notification.Show(Lang.Warning, string.Format(Lang.CannotDeleteFolder, dir)
                , Severity.Warning, null, null, null, Lang.OpenFolder,
                new RelayCommand(() => PlatformHelper.OpenFileOrUrl(dir)));
        }
    }

    private void BackupConfigFile()
    {
        if (_context.DestDir == null) return;
        var configPath = Path.Combine(_context.DestDir, "config.json");
        if (!File.Exists(configPath)) return;

        Log.Information("Found config for {modName}, backing up...", _context.ModFolderName);
        File.Move(configPath, Path.Combine(_context.TempModDir, "config.json"), true);
    }

    private void BackupI18NFolder()
    {
        if (_context.DestDir == null) return;
        var i18NPath = Path.Combine(_context.DestDir, "i18n");
        if (!Directory.Exists(i18NPath)) return;

        Log.Information("Found i18n folder for {modName}, backing up...", _context.ModFolderName);
        var tempI18NDir = Path.Combine(_context.TempModDir, "i18n");
        Directory.CreateDirectory(tempI18NDir);

        foreach (var file in Directory.GetFiles(i18NPath, "*.json", SearchOption.AllDirectories))
        {
            var newPath = Path.Combine(tempI18NDir, Path.GetFileName(file));
            File.Copy(file, newPath, true);
        }
    }

    private void RestoreModFiles()
    {
        RestoreConfigFile();
        RestoreI18NFolder();
    }

    private void RestoreConfigFile()
    {
        var tempConfigPath = Path.Combine(_context.TempModDir, "config.json");
        if (!File.Exists(tempConfigPath)) return;

        Log.Information("Found config for {modName}, restoring...", _context.ModFolderName);
        File.Move(tempConfigPath, Path.Combine(_context.ModDir, "config.json"), true);
    }

    private void RestoreI18NFolder()
    {
        var tempI18NPath = Path.Combine(_context.TempModDir, "i18n");
        if (!Directory.Exists(tempI18NPath)) return;

        Log.Information("Found i18n folder for {modName}, restoring...", _context.ModFolderName);
        var i18NHandler = new I18NFileHandler(_context.ModDir);

        foreach (var file in Directory.GetFiles(tempI18NPath)) i18NHandler.ProcessI18NFile(file);
    }

    private void CleanupTempDirectories()
    {
        if (Directory.Exists(_context.TempModDir))
            Directory.Delete(_context.TempModDir, true);
    }

    private class ModInstallContext
    {
        public ModInstallContext()
        {
        }

        public ModInstallContext(string modFolderName)
        {
            ModFolderName = modFolderName;
        }

        public bool IsNotSupportPack { get; init; } = true;
        public Manifest Manifest { get; init; } = new();
        public string ModFolderName { get; init; } = string.Empty;
        public string TempDir { get; init; } = string.Empty;
        public string TempModDir { get; init; } = string.Empty;
        public string? DestDir { get; init; } = string.Empty;
        public string ModDir { get; init; } = string.Empty;
        public string? DestDirParent { get; init; }
    }
}

public class TranslationPackInstaller(string fileName)
{
    public async Task<object?> ProcessI18NInstallation(IArchive archive)
    {
        var tempI18NFiles = Path.Combine(Services.TempDir, "ToInstallI18N");
        bool? isSuccess = false;

        try
        {
            isSuccess = await InstallI18NFiles(archive, tempI18NFiles);
        }
        catch (Exception e)
        {
            Services.Notification.Show(Lang.Warning, e.Message, Severity.Warning);
            Log.Error(e, "Error occurred during i18n translation");
        }
        finally
        {
            if (Directory.Exists(tempI18NFiles))
                Directory.Delete(tempI18NFiles, true);
        }

        return ShowI18NInstallationResult(isSuccess);
    }

    private async Task<bool?> InstallI18NFiles(IArchive archive, string tempI18NFiles)
    {
        var i18NFiles = archive.Entries
            .Where(e => e.Key != null && e.Key.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (i18NFiles.Count == 0) return false;

        archive.ExtractToDirectory(tempI18NFiles);
        bool? success = false;

        foreach (var jsonFilePath in Directory.GetFiles(tempI18NFiles, "*.json", SearchOption.AllDirectories))
        {
            success |= await ProcessSingleI18NFile(jsonFilePath);
        }

        return success;
    }

    private async Task<bool?> ProcessSingleI18NFile(string jsonFilePath)
    {
        var json = TranslationContext.GetMap(await File.ReadAllTextAsync(jsonFilePath));
        if (json.Keys.Count == 0) return false;

        var key = json.Keys.First();
        var destPath = await FindDestinationPath(key);
        if (string.IsNullOrEmpty(destPath)) return null;

        return await MergeOrCopyTranslationFile(jsonFilePath, destPath, json);
    }

    private async Task<string?> FindDestinationPath(string key)
    {
        var defaults = Directory.EnumerateFiles(Services.MainConfig.DirectoryPath,
                "default.json", SearchOption.AllDirectories)
            .Where(x => x.Contains("i18n", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (defaults.Count == 0) return null;

        foreach (var defaultFile in defaults)
        {
            var jsonContent = await File.ReadAllTextAsync(defaultFile);
            var jsonObj = TranslationContext.GetMap(jsonContent);
            if (!jsonObj.ContainsKey(key)) continue;

            var directory = Directory.GetParent(defaultFile);
            return directory?.FullName;
        }

        return null;
    }

    private static async Task<bool> MergeOrCopyTranslationFile(string jsonFilePath, string destPath,
        Dictionary<string, string> json)
    {
        var jsonFile = new FileInfo(jsonFilePath);
        var targetFilePath = Path.Combine(destPath, jsonFile.Name);

        if (!File.Exists(targetFilePath))
        {
            File.Copy(jsonFile.FullName, targetFilePath);
            return true;
        }

        try
        {
            var desJsonMap = TranslationContext.GetMap(await File.ReadAllTextAsync(targetFilePath));
            if (desJsonMap.Count == 0) return false;

            json.Where(x => !desJsonMap.ContainsKey(x.Key))
                .ForEach(x => desJsonMap.Add(x.Key, x.Value));

            var text = TranslationContext.GetJson(desJsonMap);
            await File.WriteAllTextAsync(targetFilePath, Regex.Unescape(text));
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error occurred during Merging translations");
            if (File.Exists(targetFilePath)) File.Delete(targetFilePath);
            File.Copy(jsonFile.FullName, targetFilePath);
            return true;
        }
    }

    private object? ShowI18NInstallationResult(bool? isSuccess) => isSuccess switch
    {
        true => null,
        false => Services.Notification.Show(Lang.Warning, Lang.I18NInstallFailed, Severity.Warning),
        null => Services.Notification.Show(Lang.Warning, string.Format(Lang.I18NInstallNotFound, fileName),
            Severity.Warning)
    };
}

public class I18NFileHandler(string destDir)
{
    public void ProcessI18NFile(string file)
    {
        if (Path.GetFileName(file) == "default.json") return;

        var defaultLang = File.ReadAllText(Path.Combine(destDir, "i18n", "default.json"));
        var defaultMap = TranslationContext.GetMap(defaultLang);
        var oldMap = TranslationContext.GetMap(File.ReadAllText(file));
        var newLangPath = Path.Combine(destDir, "i18n", Path.GetFileName(file));

        if (!File.Exists(newLangPath))
        {
            File.Copy(file, newLangPath, true);
            return;
        }

        MergeTranslations(newLangPath, defaultMap, oldMap);
    }

    private static void MergeTranslations(string newLangPath,
        Dictionary<string, string>? defaultMap,
        Dictionary<string, string>? oldMap)
    {
        var newMap = TranslationContext.GetMap(File.ReadAllText(newLangPath));
        if (defaultMap == null || oldMap == null ||
            newMap.Count < 1 || oldMap.Count < 1) return;

        defaultMap
            .Where(pair => !newMap.ContainsKey(pair.Key) && oldMap.ContainsKey(pair.Key))
            .Select(it => it.Key)
            .ForEach(key => newMap[key] = oldMap[key]);

        File.WriteAllText(newLangPath, TranslationContext.GetJson(newMap));
    }
}

internal enum ArchiveType
{
    Mod,
    Translation,
    Unknown
}