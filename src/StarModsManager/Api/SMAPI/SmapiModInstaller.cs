using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Serilog;
using SharpCompress.Archives;
using StarModsManager.Assets;
using StarModsManager.Trans;

namespace StarModsManager.Api.SMAPI;

public class SmapiModInstaller(string fileDir)
{
    private readonly string _targetDirectory = Services.MainConfig.DirectoryPath;
    private readonly string _tempDir = Services.TempDir;
    private ModInstallContext? _context;

    public static void Install(string fileDir) => Install([fileDir]);

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
        Services.Notification.ShowLongMsg(Lang.Installing, msg, Severity.Informational);
        
        var successMsg = new StringBuilder();
        Task.Run(async () =>
        {
            foreach (var pair in paths)
            {
                await StartInstall(pair, successMsg);
            }
        }).ContinueWith(_ =>
        {
            if (successMsg.Length == 0) return;
            Services.Notification.ShowLongMsg(Lang.ModInstallSuccess, successMsg.ToString(), Severity.Success);
        });
    }

    private static async Task StartInstall(KeyValuePair<string, IArchive> pair, StringBuilder successMsg)
    {
        var type = GetArchiveType(pair.Value.Entries);
        switch (type)
        {
            case ArchiveType.Mod:
                var installer = new SmapiModInstaller(pair.Key);
                installer.Install();
                successMsg.AppendLine(string.Format(Lang.InstalledModWithVersion,
                    installer._context!.ModFolderName, installer._context.Manifest.Version));
                break;
            case ArchiveType.Translation:
                var fileName = new FileInfo(pair.Key).Name;
                var tansInstaller = new TranslationPackInstaller(fileName);
                var result = await tansInstaller.ProcessI18NInstallation(pair.Value);
                if (result is null) successMsg.AppendLine(string.Format(Lang.I18NInstallSuccess, fileName));
                break;
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

    private void Install()
    {
        try
        {
            using var archive = ArchiveFactory.Open(fileDir);
            _context = ValidateAndCreateContext(archive);
            if (_context == null) return;

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
    }

    private ModInstallContext? ValidateAndCreateContext(IArchive archive)
    {
        var manifestEntry = archive.Entries.FirstOrDefault(e =>
            e.Key != null && e.Key.EndsWith("manifest.json", StringComparison.OrdinalIgnoreCase));

        if (manifestEntry != null) return CreateModContext(manifestEntry);

        Services.Notification.Show(Lang.NotModArchive, Lang.ManifestFileNotFound, Severity.Warning);
        return null;
    }

    private ModInstallContext CreateModContext(IArchiveEntry manifestEntry)
    {
        var manifest = JsonSerializer.Deserialize(manifestEntry.OpenEntryStream(), ManifestContent.Default.Manifest)!;
        var modFolderName = Path.GetDirectoryName(manifestEntry.Key) ?? Path.GetFileNameWithoutExtension(fileDir);
        var tempDir = Path.Combine(_tempDir, "ToInstallMod");
        var tempModDir = Path.Combine(_tempDir, modFolderName);
        var sourceDir = Path.Combine(tempDir, modFolderName);
        var destDir = GetDestinationDirectory(manifest, modFolderName);

        return new ModInstallContext
        {
            Manifest = manifest,
            ModFolderName = modFolderName,
            TempDir = tempDir,
            TempModDir = tempModDir,
            SourceDir = sourceDir,
            DestDir = destDir
        };
    }

    private string GetDestinationDirectory(Manifest manifest, string modFolderName)
    {
        var destDir = Path.Combine(_targetDirectory, modFolderName);
        var local = ModsHelper.Instance.LocalModsMap.Values
            .FirstOrDefault(mod => mod.Manifest.UniqueID == manifest.UniqueID);

        return local?.PathS ?? destDir;
    }

    private void CreateTempDirectories()
    {
        if (_context == null) return;
        Directory.CreateDirectory(_context.TempDir);
        Directory.CreateDirectory(_context.TempModDir);
    }

    private void ProcessModInstallation(IArchive archive)
    {
        if (_context == null) return;

        archive.ExtractToDirectory(_context.TempDir);

        if (Directory.Exists(_context.DestDir)) HandleExistingMod();

        MoveDirectory(Directory.Exists(_context.SourceDir) ? _context.SourceDir : _context.TempDir, _context.DestDir);

        if (Directory.EnumerateDirectories(_context.TempDir).Any()) RestoreModFiles();

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

    private void HandleExistingMod()
    {
        if (_context == null) return;

        Log.Information("Mod already exists: {Dir}", _context.DestDir);
        _context.DestDir.CreateZipBackup(_context.ModFolderName);

        BackupConfigFile();
        BackupI18NFolder();

        Directory.Delete(_context.DestDir, true);
    }

    private void BackupConfigFile()
    {
        if (_context == null) return;

        var configPath = Path.Combine(_context.DestDir, "config.json");
        if (!File.Exists(configPath)) return;

        Log.Information("Found config for {modName}, backing up...", _context.ModFolderName);
        File.Move(configPath, Path.Combine(_context.TempModDir, "config.json"), true);
    }

    private void BackupI18NFolder()
    {
        if (_context == null) return;

        var i18NPath = Path.Combine(_context.DestDir, "i18n");
        if (!Directory.Exists(i18NPath)) return;

        Log.Information("Found i18n folder for {modName}, backing up...", _context.ModFolderName);
        var files = Directory.GetFiles(i18NPath, "*", SearchOption.AllDirectories);
        var tempI18NDir = Path.Combine(_context.TempModDir, "i18n");
        Directory.CreateDirectory(tempI18NDir);

        foreach (var file in files)
        {
            var newPath = Path.Combine(tempI18NDir, Path.GetFileName(file));
            File.Copy(file, newPath, true);
        }
    }

    private void RestoreModFiles()
    {
        if (_context == null) return;
        RestoreConfigFile();
        RestoreI18NFolder();
    }

    private void RestoreConfigFile()
    {
        if (_context == null) return;

        var tempConfigPath = Path.Combine(_context.TempModDir, "config.json");
        if (!File.Exists(tempConfigPath)) return;

        Log.Information("Found config for {modName}, restoring...", _context.ModFolderName);
        File.Move(tempConfigPath, Path.Combine(_context.DestDir, "config.json"), true);
    }

    private void RestoreI18NFolder()
    {
        if (_context == null) return;

        var tempI18NPath = Path.Combine(_context.TempModDir, "i18n");
        if (!Directory.Exists(tempI18NPath)) return;

        Log.Information("Found i18n folder for {modName}, restoring...", _context.ModFolderName);
        var i18NHandler = new I18NFileHandler(_context.DestDir);

        foreach (var file in Directory.GetFiles(tempI18NPath)) i18NHandler.ProcessI18NFile(file);
    }

    private void CleanupTempDirectories()
    {
        if (_context == null) return;

        if (Directory.Exists(_context.TempDir))
            Directory.Delete(_context.TempDir, true);
        if (Directory.Exists(_context.TempModDir))
            Directory.Delete(_context.TempModDir, true);
    }

    private class ModInstallContext
    {
        public required Manifest Manifest { get; init; }
        public required string ModFolderName { get; init; }
        public required string TempDir { get; init; }
        public required string TempModDir { get; init; }
        public required string SourceDir { get; init; }
        public required string DestDir { get; init; }
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
        null => Services.Notification.Show(Lang.Warning, string.Format(Lang.I18NInstallNotFound, fileName), Severity.Warning)
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

enum ArchiveType
{
    Mod,
    Translation,
    Unknown
}