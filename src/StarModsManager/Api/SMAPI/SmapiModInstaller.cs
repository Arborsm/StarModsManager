using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Serilog;
using StarModsManager.Trans;

namespace StarModsManager.Api.SMAPI;

public class SmapiModInstaller(string fileDir)
{
    private readonly string _targetDirectory = Services.MainConfig.DirectoryPath;
    private readonly string _tempDir = Services.TempDir;
    private ModInstallContext? _context;
    private object? _msg;

    public static void Install(string fileDir)
    {
        var installer = new SmapiModInstaller(fileDir);
        installer.Install();
    }

    public void Install()
    {
        try
        {
            _msg = Services.Notification.Show("正在安装...", 
                $"正在尝试安装{Path.GetFileNameWithoutExtension(fileDir)}", Severity.Informational);
            
            using var archive = ZipFile.OpenRead(fileDir);
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

    private class ModInstallContext
    {
        public required Manifest Manifest { get; init; }
        public required string ModFolderName { get; init; }
        public required string TempDir { get; init; }
        public required string TempModDir { get; init; }
        public required string SourceDir { get; init; }
        public required string DestDir { get; init; }
    }

    private ModInstallContext? ValidateAndCreateContext(ZipArchive archive)
    {
        var manifestEntry = archive.Entries.FirstOrDefault(e =>
            e.Name.Equals("manifest.json", StringComparison.OrdinalIgnoreCase));

        if (manifestEntry == null)
        {
            Services.Notification.Show("非Mod压缩包", "未找到manifest.json文件", Severity.Warning);
            return null;
        }

        var manifest = JsonSerializer.Deserialize(manifestEntry.Open(), ManifestContent.Default.Manifest)!;
        var modFolderName = Path.GetDirectoryName(manifestEntry.FullName) 
            ?? Path.GetFileNameWithoutExtension(fileDir);

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

    private void ProcessModInstallation(ZipArchive archive)
    {
        if (_context == null) return;
        
        archive.ExtractToDirectory(_context.TempDir, true);
        
        if (Directory.Exists(_context.DestDir)) HandleExistingMod();

        MoveDirectory(Directory.Exists(_context.SourceDir) ? _context.SourceDir : _context.TempDir, _context.DestDir);

        if (Directory.EnumerateDirectories(_context.TempDir).Any()) RestoreModFiles();

        Log.Information("Mod installed successfully: {Dir}", _context.ModFolderName);
        
        Services.Notification.Close(_msg);
        Services.Notification.Show("Mod 安装成功", $"已安装 {_context.ModFolderName}({_context.Manifest.Version})", Severity.Success);
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
        
        foreach (var file in Directory.GetFiles(tempI18NPath))
        {
            i18NHandler.ProcessI18NFile(file);
        }
    }

    private void CleanupTempDirectories()
    {
        if (_context == null) return;
        
        if (Directory.Exists(_context.TempDir)) 
            Directory.Delete(_context.TempDir, true);
        if (Directory.Exists(_context.TempModDir)) 
            Directory.Delete(_context.TempModDir, true);
    }
}

public class I18NFileHandler(string destDir)
{
    public void ProcessI18NFile(string file)
    {
        if (Path.GetFileName(file) == "default.json") return;

        var json = TranslationContext.Default.DictionaryStringString;
        var defaultLang = File.ReadAllText(Path.Combine(destDir, "i18n", "default.json"));
        var defaultMap = JsonSerializer.Deserialize(defaultLang, json);
        var oldMap = JsonSerializer.Deserialize(File.ReadAllText(file), json);
        var newLangPath = Path.Combine(destDir, "i18n", Path.GetFileName(file));

        if (!File.Exists(newLangPath))
        {
            File.Copy(file, newLangPath, true);
            return;
        }

        MergeTranslations(newLangPath, defaultMap, oldMap, json);
    }

    private static void MergeTranslations(string newLangPath, 
        Dictionary<string, string>? defaultMap, 
        Dictionary<string, string>? oldMap,
        JsonTypeInfo<Dictionary<string, string>> json)
    {
        var newMap = JsonSerializer.Deserialize(File.ReadAllText(newLangPath), json);
        if (defaultMap == null || newMap == null || oldMap == null || 
            newMap.Count < 1 || oldMap.Count < 1) return;

        defaultMap
            .Where(pair => !newMap.ContainsKey(pair.Key) && oldMap.ContainsKey(pair.Key))
            .Select(it => it.Key)
            .ForEach(key => newMap[key] = oldMap[key]);

        File.WriteAllText(newLangPath, JsonSerializer.Serialize(newMap, json));
    }
}